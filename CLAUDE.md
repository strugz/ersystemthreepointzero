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

### ERFWeb (standalone web ERF system)

`ERFWeb/` is a third, separate application: ASP.NET Core (net10.0) + EF Core on **PostgreSQL** (Npgsql, snake_case, LINQ only, no stored procedures) and a Vue 3 + Vuetify SPA. It is not in `ER System.sln`, the installer, or `Web/`, and current work is scoped to it: do not modify the desktop app while working there. `ERFWeb/README.md` is its reference (configuration, database scripts, features by plan phase, deployment); the governing plan is `ERFWeb/Document/ERFWeb - Project Documentation and Implementation Plan.docx`.

- **Verify:** from `ERFWeb/Backend`, `dotnet build ERFWeb.sln`, `dotnet test ERFWeb.sln`, `dotnet format ERFWeb.sln --verify-no-changes` (warnings are errors here too). From `ERFWeb/Frontend/erfweb-client`, `npm run lint` and `npm run build`; there are no frontend unit tests. The `/verify-web` and `/verify-frontend` skills target `Web/`, not ERFWeb.
- **Configuration:** only `ERFWeb/Backend/src/ERFWeb.Api/appsettings.json` (gitignored; template `appsettings.Example.json`) plus `appsettings.{Environment}.json`. `Program.cs` clears every other source, so environment variables and user secrets are ignored. `Lasius` (SMS) and `Smtp` (email) are optional sections; the API starts without them and those features answer 503. Never print or commit its contents.
- **Database:** numbered forward scripts `ERFWeb/Database/NNN_*.sql` (idempotent, commented rollback at the end), applied by the user by hand to `erms_db`. Add a new numbered script for schema changes; never run `dotnet ef database update` against it. Read-only checks against it are fine.
- **Deployment:** follow the "Deployment guide" in `ERFWeb/README.md`. Backend: `Backend/tools/Publish-ErfWeb.cmd` builds `ERFWeb/releases/ERFWeb-Api-<version>-<commit>/`, and `Deploy-ErfWeb.cmd` in that folder installs it on the server (Windows Service `ERFWeb.Api` in `C:\inetpub\wwwroot\erf_web-app`, behind nginx at `https://sr.mdmpi.com.ph/erf/`, which maps `/erf/*` to `/api/*`) with backup, health check and automatic rollback. Never copy single DLLs: a missing dependency fails only the endpoints that load it. Releases never contain `appsettings.json`; the server keeps its own. Frontend: Render static site `https://ersystemthreepointzero.onrender.com`, built from `master`; deploy the backend first. The local SPA `.env` proxies to the deployed API, so new endpoints 404 locally until deployed.
- **Local API runs:** the local `appsettings.json` binds Kestrel to the server's LAN address, so run the built DLL with a scratch content root and an `appsettings.<Env>.json` Kestrel override (`.claude/launch.json` has `erfweb-client-local-api` for the SPA side, port 5179 → localhost:5081).
- **Notifications:** `IApprovalNotifier` is layered Logging → SMS (Lasius) → Email (MailKit, R47 attached). Delivery runs after the workflow commits and never fails it; each send is claimed once per idempotency key in `sms_deliveries` / `email_deliveries`, and only counts (never numbers, addresses or text) go to logs and `workflow_audit`.

### Other agent configuration

`.github/copilot-instructions.md` and `.codex/` exist for Copilot and Codex. Their guidance is already covered by `AGENTS.md`; nothing needs to be mirrored here.
