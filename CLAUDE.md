@AGENTS.md

<!--
Maintainer note: AGENTS.md is the single source of truth for repo-wide agent rules
and is shared with Codex and Copilot. Do not copy its content here — the import
above loads it in full. Add only Claude-Code-specific facts below.
Nested guides load automatically: Web/CLAUDE.md, Web/Backend/CLAUDE.md, and
Web/Frontend/CLAUDE.md each import their sibling AGENTS.md on demand.
-->

## Claude Code

### Shell and scripts

PowerShell is the primary shell on this machine. Repository scripts under `tools/` are run as:

```
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\<script>.ps1
```

`tools/Invoke-WebCleanArchitectureAudit.ps1` (report-only) and `tools/Verify-ScannedReceipts.ps1` (read-only) are safe to run. `tools/Register-WebCleanArchitectureAudit.ps1` writes to Windows Task Scheduler and must only be run when explicitly requested.

### Build and test entry points

Use the verification skills rather than assembling these commands by hand:

- `/verify-desktop` — restore, build `ER System.sln`, run the MSTest suite
- `/verify-web` — restore, build, test, and format-check `Web/Backend/ERSystem.Web.sln`
- `/verify-frontend` — `npm ci`, lint, test, build the Vue client

### Toolchain facts that are easy to get wrong

- **Desktop tests are MSTest on .NET Framework 4.8.** Run them with `vstest.console.exe`. `dotnet test` does not work against `ERSystem.Tests`.
- **Desktop projects use `packages.config`, not SDK-style PackageReference.** Restore with `nuget restore "ER System.sln"`; `dotnet restore` is not a substitute.
- **`ERSystem3.5Setup/ERSystem3.5Setup.vdproj` cannot be built by MSBuild.** It requires `devenv.com`. Exclude it from solution builds and treat installer validation as a separate, explicit step.
- **The web backend sets `TreatWarningsAsErrors=true`** (`Web/Backend/Directory.Build.props`). Any new compiler warning fails the build.
- **Resolve Visual Studio paths at runtime with `vswhere`**, never hardcode them:
  ```
  & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
  ```
  Verified-good local versions: .NET SDK 10.0.400, Node 24.18.0 / npm 11.16.0, Visual Studio 18. Pin machine-specific absolute paths in `CLAUDE.local.md`, not here.

### Rules most often violated silently

These are already in `AGENTS.md` but bear repeating because the failure is quiet:

- No new business logic or SQL in WinForms code-behind, Vue components, or API controllers.
- Never execute `ER3.0.sql` wholesale and never auto-apply EF Core migrations. The web application does not own the legacy schema; database changes go in a new dated script under `Database/`.
- Do not revert or clean up unrelated user changes in the working tree.

### Clean up superseded code

When an approach is revised mid-task (a first attempt did not match the requirement and a new function, component, prop, endpoint or test replaced it), remove what the new version made obsolete in the same change. Before reporting done:

- Review the full diff (`git diff`), not just the last edit, and check every function, variable, prop, emit, import, type, CSS class, test and doc line this task added is still used. Search for callers before keeping or deleting anything.
- Delete superseded code instead of leaving it beside its replacement; do not keep "old" or "v2" variants, commented-out blocks, or unused helpers.
- Remove scratch files, debug logging and temporary test data created during the task.
- Check that untouched lines did not change incidentally (line endings, formatting, encoding).
- This applies only to code this task introduced or changed. Pre-existing unused code is out of scope unless the user asks; mention it instead.

### End-of-turn change summary

Whenever a turn changes files, end the final message with a **Changed files** section:

- Group by area (Backend, Frontend, Database, Docs, Tests) and list every changed file as a clickable link, with one line on what changed in it. Say plainly when an area was not touched (e.g. "Backend: no changes").
- Follow it with **What happens next**: what the user will see or what behaves differently, what is still needed before it takes effect (database script to apply, backend deploy before frontend, config), and what was verified versus not verified (build, tests, browser).
- State whether anything is committed, pushed or deployed.

### Keep README.md and CLAUDE.md in step with every change

Every change that adds, removes or changes behaviour, an endpoint, a setting, a database script, a screen, a deployment step or a rule also updates, in the same change (same commit or PR):

- **The area's README.** `ERFWeb/README.md` for ERFWeb, `Web/README.md` for the web portal, `README.md` for the desktop app. Update the section that describes the feature, and for ERFWeb add a dated line to its **Change log** (newest first; say "not yet committed" until it is, and the PR number once it has one).
- **This file.** Add a dated line to **Recent changes** below, and update any fact here that the change made stale (configuration sections, database rules, deployment, integrations).
- Before reporting done, check both files against the diff. A change is not finished while either file describes the old behaviour.

### ERFWeb (standalone web ERF system)

`ERFWeb/` is a third, separate application: ASP.NET Core (net10.0) + EF Core on **PostgreSQL** (Npgsql, snake_case, LINQ only, no stored procedures) and a Vue 3 + Vuetify SPA. It is not in `ER System.sln`, the installer, or `Web/`, and current work is scoped to it: do not modify the desktop app while working there. `ERFWeb/README.md` is its reference (configuration, database scripts, features by plan phase, deployment); the governing plan is `ERFWeb/Document/ERFWeb - Project Documentation and Implementation Plan.docx`.

- **Verify:** from `ERFWeb/Backend`, `dotnet build ERFWeb.sln`, `dotnet test ERFWeb.sln`, `dotnet format ERFWeb.sln --verify-no-changes` (warnings are errors here too). From `ERFWeb/Frontend/erfweb-client`, `npm run lint` and `npm run build`; there are no frontend unit tests. The `/verify-web` and `/verify-frontend` skills target `Web/`, not ERFWeb. Tests never use a database: the RBAC guardrails 9.1–9.18 are covered by the Domain rule tests and `tests/ERFWeb.Tests/Architecture/GuardrailCodeTests` (policies, read-only audit endpoint, no code that edits audit rows). Do not add a test database or Testcontainers; the user decided against it.
- **Configuration:** only `ERFWeb/Backend/src/ERFWeb.Api/appsettings.json` (gitignored; template `appsettings.Example.json`) plus `appsettings.{Environment}.json`. `Program.cs` clears every other source, so environment variables and user secrets are ignored. `Lasius` (SMS), `Smtp` (email) and `Crm` (CRM employee lookup) are optional sections; the API starts without them and those features answer 503. `Reminders` (`Enabled`, `RunAt`) is optional too and off unless `Enabled` is `true`, so only the server sends SLA reminders. Never print or commit its contents.
- **Database:** numbered forward scripts `ERFWeb/Database/NNN_*.sql` (idempotent, commented rollback at the end), applied by the user by hand to `erms_db`. Add a new numbered script for schema changes; never run `dotnet ef database update` against it. Read-only checks against it are fine. Since script `021`, `workflow_audit` is append-only in the database (a trigger refuses UPDATE, DELETE and TRUNCATE); a maintenance script that must delete audit rows opts in with `SET LOCAL erfweb.allow_audit_purge = 'on'`, and application code never does.
- **Deployment:** follow the "Deployment guide" in `ERFWeb/README.md`. Backend: `Backend/tools/Publish-ErfWeb.cmd` builds `ERFWeb/releases/ERFWeb-Api-<version>-<commit>/`, and `Deploy-ErfWeb.cmd` in that folder installs it on the server (Windows Service `ERFWeb.Api` in `C:\inetpub\wwwroot\erf_web-app`, behind nginx at `https://sr.mdmpi.com.ph/erf/`, which maps `/erf/*` to `/api/*`) with backup, health check and automatic rollback. Never copy single DLLs: a missing dependency fails only the endpoints that load it. Releases never contain `appsettings.json`; the server keeps its own. Frontend: Render static site `https://ersystemthreepointzero.onrender.com`, built from `master`; deploy the backend first. The local SPA `.env` proxies to the deployed API, so new endpoints 404 locally until deployed.
- **Local API runs:** the local `appsettings.json` binds Kestrel to the server's LAN address, so run the built DLL with a scratch content root and an `appsettings.<Env>.json` Kestrel override (`.claude/launch.json` has `erfweb-client-local-api` for the SPA side, port 5179 → localhost:5081).
- **Notifications:** `IApprovalNotifier` is layered Logging → SMS (Lasius) → Email (MailKit, R47 attached). Delivery runs after the workflow commits and never fails it; each send is claimed once per idempotency key in `sms_deliveries` / `email_deliveries`, and only counts (never numbers, addresses or text) go to logs and `workflow_audit`. Verification steps set `WorkflowEventNotification.DeliveryKey` and go only to their `RecipientUserIds` (`ISmsService.SendToUserAboutReportAsync` per person, `IReportEmailService.SendToUsersAboutReportAsync` once), not to the claimant and signatories. Daily SLA reminders (Phase 8.4) are separate: `ApprovalReminderJob` (a `BackgroundService` in the API) runs `ApprovalReminderService` once a working day; it uses the SMS and SMTP gateways directly (there is no signed-in user) and claims each reminder once in `reminder_deliveries`. Timing rules are in Domain `ApprovalReminderRules` and the Settings SLA days.
- **Backups and monitoring (Phase 8.6):** not part of the API. Two Windows scheduled tasks on the API server, set up with `Backend/tools/Install-ErfWebMaintenance.cmd` (settings at its top) and documented step by step in `Backend/tools/README.txt`: a nightly `pg_dump` (`Backup-ErfWebDatabase.ps1`) and a 5-minute check of `/erf/health` and the backup's age (`Test-ErfWebHealth.ps1`), both reading the DB login and `Smtp` from the server's `appsettings.json`. PostgreSQL 18 runs on a different server from the API; the API server uses pgAdmin's `runtime` folder for `pg_dump`/`pg_restore`. These are Windows PowerShell 5.1 scripts: keep them ASCII-only and `.cmd` files CRLF.
- **CRM:** an account links to the CRM by the CRM `employeeId` in `users.crm_employee_id` (the CRM has no employee email); CRM activity calls take that ID. An expense line pre-filled from CRM activities stores their `activityId`s in `expense_lines.crm_activity_ids` (several per line when one visit served several instruments; their service no., instrument and serial are joined with " / "; one activity: many lines of one report, never a second non-Rejected report; `CrmActivityClaimCheck`). The old `crm_activity_id` column is emptied by script `023` and no longer read. Only the ERFWeb server calls the CRM (`CrmEmployeeDirectory`, `CrmActivityDirectory`, shared `CrmApi`, `X-Api-Key`), logging only HTTP status, never search text, IDs or the key. The contract is `ERFWeb/Document/CRM-API-Integration-Spec.md` plus the CRM team's "CRM Employee and Activity API for ERFWeb" doc; follow their field names exactly (e.g. `mobileNumber`).
- **Cost center budgets:** a cost center is one department's (one active per department) with one overall budget per calendar year (`cost_center_budgets`). Monitoring only: never block filing or approval on it and never print it on the R47. Spending counts against `expense_reports.department_id`, set at filing. Finance sees every cost center; a signatory those of the departments their Department or Exclusion scope covers today (`CostCenterBudgetService`).
- **R47 Work With:** group headers print `<location> Work w/: <initials>` (Domain `R47Layout.WorkWithInitials`); full Work With names never print on the R47. Line order on the R47 is the saved `expense_lines.sort_order`, set by the editor's Sort menu through `PUT /api/reports/{id}/lines/order`.

### Recent changes

Newest first; the full entries are in the **Change log** of `ERFWeb/README.md`.

- **2026-10-27:** R47 ENDORSED BY and APPROVED BY signatures print above the names, not over them (not yet committed).
- **2026-10-27:** **Approve** on the R47 PDF page when opened from a review and the viewer may approve (PR #33).
- **2026-10-27:** One expense line can link several CRM activities (checkboxes in the From CRM picker; service no., instrument and serial joined with " / "; script `023`, `SchemaVersion.Required` 23) (PR #32).
- **2026-10-27:** Scope assignments can be edited in full (signatory, level, type, department, effective-from), not only ended and re-created (PR #32).
- **2026-10-26:** Verification steps send SMS and email to the claimant, Expense Verifiers, Finance and Level 2 as each step concerns them (PR #32). Cost center budgets: one cost center per department with a yearly budget, a **Budgets** page for signatories and Finance, filing department saved on the report (script `022`, `SchemaVersion.Required` 22; PR #31). R47 Work With prints as initials (PR #30). Sort expense lines into the R47 order (PR #29). CRM picker previous/next day arrows and File report / Re-file report from the R47 preview (PR #28). Phase 8.4 SLA reminders, 8.5 guardrail tests and append-only `workflow_audit` (script `021`), 8.6 nightly backup and health monitor (PR #27).

### Other agent configuration

`.github/copilot-instructions.md` and `.codex/` exist for Copilot and Codex. Their guidance is already covered by `AGENTS.md`; nothing needs to be mirrored here.
