# ER System Web Portal

This solution adds Manager approval and Finance physical-receipt tracking beside the existing ER System 3.0 desktop application. It uses the same SQL Server database but is intentionally separate from `ER System.sln` and the desktop installer.

## Prerequisites

- .NET 10 SDK and ASP.NET Core Hosting Bundle on IIS.
- Node.js 20.19 or later for frontend builds.
- SQL Server 2008 or later with the ER database kept at compatibility level 100. SQL Server versions before 2019 run in legacy compatibility mode and require full workflow testing against the deployed database.
- A least-privilege SQL login with only the permissions required by the mapped tables and legacy procedures.

## Database preparation

1. Back up the ER database.
2. Review and run `Database/20260715_CreateWebApprovalAndReceiptSupport.sql` in a controlled deployment window.
3. Stop if any precheck fails. Resolve the reported duplicate or orphaned legacy data through an approved data-correction process; the script deliberately does not merge or delete it.
4. Retain `Database/20260715_RollbackWebApprovalAndReceiptSupport.sql` with the deployment package.
5. Review and run `Database/20260720_CreateApprovalReminderSupport.sql` before installing the reminder service. It adds the nullable reminder email field, the delivery ledger, and the backward-compatible `REMINDER` branch of `dbo.sp_Notify`.
6. Retain `Database/20260720_RollbackApprovalReminderSupport.sql` for emergency rollback. It restores the prior two-parameter `dbo.sp_Notify` contract and removes the reminder delivery ledger, but deliberately preserves `NotificationEmail` contact data.
7. Review and run `Database/20260724_AddApprovalActivationEmailSupport.sql`. It enables occurrence zero for one-time activation email and marks approvals already actionable during deployment so they do not receive a misleading filing email.
8. Retain `Database/20260724_RollbackApprovalActivationEmailSupport.sql`. It removes only activation-email delivery records and restores the original positive reminder-number constraint.

Never run `ER3.0.sql` as a migration. It is reference material for the current live schema.

## Backend configuration

Keep secrets outside source control. For local development, use .NET user secrets; for IIS, use protected environment or application settings.

```text
ConnectionStrings__ErDatabase=<SQL Server connection string>
LegacyAuthentication__EncryptionKey=<existing ER credential encryption key>
```

The API validates the SQL Server major version and database compatibility level during startup. It does not apply migrations automatically.

## Approval reminder Windows Service

`ERSystem.Reminders.Worker` is published and installed separately from IIS. Windows Service Manager starts it automatically, and it continues working when the desktop application and Web API are closed.

When an employee files a report, the existing `sp2_RefileER` and `dbo.sp_Notify 'FILE'` path still creates the legacy SMS gateway request. The worker polls the current actionable approval every 60 seconds and sends a one-time activation email to the manager recipient and employee. When a later approval step becomes actionable, the worker sends another activation email using the employee's same configured manager address. The worker does not queue another SMS during activation, so the legacy `FILE` notification is not duplicated.

For email delivery, `EmployeeUserID` on the actionable `tbReportApprovalTransaction` row identifies the employee's `tbUserRegistration` record. The worker decrypts that row's `EmailAdd` and `EmailPass` in application memory using the existing legacy encryption key. It uses the decrypted `EmailAdd` as the SMTP username, sender, and employee recipient. The employee row's plain-text `EmailTo` is the manager recipient. `ApproverUserID` continues to control approval ownership, manager naming, claim revalidation, and the SMS identity; it does not select the email address. The scoped mailbox lookup is discarded after each worker scan and no address, password, ciphertext, or key is logged or written to the reminder delivery table.

The scheduled process runs daily at 8:00 AM Manila time. It sends manager email, employee email, and one combined `dbo.sp_Notify 'REMINDER'` SMS request on the third local calendar day, then on every Wednesday while the approval remains actionable. When day three is Wednesday, only the day-three occurrence is sent. A missed run sends only the latest due occurrence.

Production secrets belong in an ACL-protected JSON file outside the repository and publish directory. Start from this shape:

The committed worker configuration contains a blank database connection string by design. If a real credential was previously committed or distributed with a publish folder, rotate that credential before deploying this version.

```json
{
  "ConnectionStrings": {
    "ErDatabase": "<SQL Server connection string>"
  },
  "LegacyAuthentication": {
    "EncryptionKey": "<existing ER credential encryption key>"
  },
  "ApprovalReminders": {
    "EmailEnabled": false,
    "SmsEnabled": false,
    "ActivationPollIntervalSeconds": 60,
    "RunAtLocalTime": "08:00",
    "TimeZoneId": "Asia/Manila",
    "InitialDelayDays": 3,
    "ReminderDayOfWeek": "Wednesday"
  },
  "Smtp": {
    "Host": "mail.marsmandrysdale.com",
    "Port": 25,
    "TlsMode": "None",
    "SenderDisplayName": "ER System"
  }
}
```

`Smtp:Username`, `Smtp:Password`, and `Smtp:SenderAddress` are intentionally not worker settings. Those values are resolved per employee from `EmailAdd` and `EmailPass`. Port 25 with `TlsMode: None` matches the current desktop behavior; do not change it to StartTLS until the deployed mail server has been tested and confirmed to support it.

Publish and install from an elevated PowerShell session:

```powershell
.\tools\Publish-ErReminderService.ps1 -Configuration Release -OutputPath C:\Services\ERSystemApprovalReminders
.\tools\Manage-ErReminderService.ps1 -Action Install `
  -ExecutablePath C:\Services\ERSystemApprovalReminders\ERSystem.Reminders.Worker.exe `
  -SettingsPath C:\ProgramData\ERSystem\ApprovalReminders\appsettings.Production.json
```

The installation script creates a delayed automatic service under the dedicated `NT SERVICE\ERSystemApprovalReminders` virtual account, grants it read-only configuration access, registers the Event Log source, and configures process restart after unexpected failure. Grant the service identity only the required SQL permissions and network access to SQL Server and SMTP. It does not need access to `D:\ERSHARE`; `dbo.sp_Notify` writes the SMS gateway file on SQL Server.

`EmailEnabled` controls activation and scheduled email. `SmsEnabled` controls only scheduled `REMINDER` SMS; it deliberately does not disable the existing legacy `FILE` SMS. When email is disabled, activation occurrences are recorded as skipped so they are not sent later as stale filing messages. Scheduled scans still report real candidate and due counts when both reminder channels are disabled.

Deploy initially with both reminder channels disabled. Verify a pilot employee's encrypted `EmailAdd` and `EmailPass` and plain-text `EmailTo`, then enable email for that controlled test. Enable SMS only after a staging `REMINDER` confirms the gateway payload. The delivery table records `Sent` for SMTP and `Queued` for the gateway; it does not claim carrier delivery acknowledgement. `NotificationEmail` remains in the database for backward compatibility but is not used by this reminder workflow.

## Local development

Backend, from `Web/Backend`:

```text
dotnet restore ERSystem.Web.sln
dotnet run --project src/ERSystem.Web.Api
```

Frontend, from `Web/Frontend/ersystem-web-client`:

```text
npm ci
npm run dev
```

Vite proxies `/api` and `/health` to the API development endpoint configured in `vite.config.ts`.

## Build and deployment

Run the checks documented in `Web/AGENTS.md`, then create the deployment output:

```text
npm --prefix Web/Frontend/ersystem-web-client ci
npm --prefix Web/Frontend/ersystem-web-client run build
dotnet publish Web/Backend/src/ERSystem.Web.Api/ERSystem.Web.Api.csproj -c Release -o <publish-directory>
```

Copy the contents of `Web/Frontend/ersystem-web-client/dist/` into `<publish-directory>/wwwroot/`. Deploy that directory as one HTTPS IIS application so the SPA and `/api` share an origin. Configure IIS to keep Secure cookies, forward HTTPS correctly, and deny direct access to configuration or deployment files.

Pilot with selected Manager and Finance accounts while the desktop client remains active. Monitor authentication failures, `409` concurrency responses, workflow audit events, API errors, and SQL latency before expanding access.

## Render deployment

The repository includes a Render Blueprint at `render.yaml` for the Vue
frontend only. Render creates a free Static Site, runs the reproducible frontend
build, publishes `dist/`, and rewrites client-side routes to `index.html`.

Create a Render Blueprint from the repository. No database connection string,
legacy encryption key, or other backend secret belongs in this static-site
deployment. `Web/Frontend/ersystem-web-client/.env.render.example` lists the
non-secret Render build settings if the site is configured manually instead of
through the Blueprint.

The current frontend API client uses relative `/api` URLs and secure,
same-origin cookie authentication by default. For the current LAN-only test,
the Render build sets `VITE_API_BASE_URL=https://192.168.4.206:5080`. The
testing browser must be connected to the `192.168.4.x` network and must trust
the backend HTTPS certificate.

Direct calls from the Render origin also require the backend to allow the exact
Render site origin with credentials. The production design should restore a
same-origin `/api` proxy or deliberately configure CORS, cookie `SameSite`
behavior, antiforgery, allowed origins, and credentials for cross-origin use.

Automatic deploys are disabled in the Blueprint for the initial frontend
preview. Enable deploys on passing checks after the Render URL, Vue Router
navigation, responsive layout, and static asset loading are verified.
