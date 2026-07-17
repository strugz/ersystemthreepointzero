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

Never run `ER3.0.sql` as a migration. It is reference material for the current live schema.

## Backend configuration

Keep secrets outside source control. For local development, use .NET user secrets; for IIS, use protected environment or application settings.

```text
ConnectionStrings__ErDatabase=<SQL Server connection string>
LegacyAuthentication__EncryptionKey=<existing ER credential encryption key>
```

The API validates the SQL Server major version and database compatibility level during startup. It does not apply migrations automatically.

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
