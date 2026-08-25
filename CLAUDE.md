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

### Other agent configuration

`.github/copilot-instructions.md` and `.codex/` exist for Copilot and Codex. Their guidance is already covered by `AGENTS.md`; nothing needs to be mirrored here.
