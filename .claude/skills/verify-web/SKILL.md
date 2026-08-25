---
name: verify-web
description: Verify the ER System ASP.NET Core web backend (Web/Backend/ERSystem.Web.sln, net10.0) by running dotnet restore, build, test, and format --verify-no-changes. Use after changing anything under Web/Backend/, or when asked to build or test the web API, the reminders worker, or the web backend solution.
---

# Verify the ER System web backend

Runs the backend verification sequence required by `Web/Backend/AGENTS.md`. Report honestly which steps ran and which were blocked — never claim success for a step that did not execute.

## Commands

Run these from the repository root, in order. Stop at the first failure and report it. The solution path is passed explicitly so no `cd` is needed.

```bash
dotnet restore "Web/Backend/ERSystem.Web.sln"
```

```bash
dotnet build "Web/Backend/ERSystem.Web.sln"
```

```bash
dotnet test "Web/Backend/ERSystem.Web.sln"
```

```bash
dotnet format "Web/Backend/ERSystem.Web.sln" --verify-no-changes
```

## Rules

- **`TreatWarningsAsErrors=true`** in `Web/Backend/Directory.Build.props`. Any new compiler warning is a build failure — treat a warning as a real defect to fix, not noise to suppress. `Nullable` and `ImplicitUsings` are also enabled, `LangVersion` is `latest`.
- **Target framework is `net10.0`.** Verified working: .NET SDK 10.0.400. There is no `global.json`, so the newest installed SDK is used; if a build fails on an SDK mismatch, report the SDK version found.
- **`dotnet format --verify-no-changes` reports only** — it does not rewrite files. If it fails, run `dotnet format "Web/Backend/ERSystem.Web.sln"` to apply the fixes, then confirm the diff is limited to files you already touched.
- **Never run `dotnet ef database update` or `dotnet ef migrations`.** This is blocked by a deny rule and by `AGENTS.md`: `LegacyErDbContext` maps the legacy schema and does not own migrations. Schema changes go in a new dated script under `Database/`.
- Do not add the web projects to `ER System.sln` or to the `ERSystem3.5Setup` installer. The desktop and web solutions are intentionally separate.

## What the test run covers

`Web/Backend/tests/ERSystem.Web.Tests` holds unit, architecture, and SQL Server integration tests. The integration tests need a reachable SQL Server; if they are skipped or fail on connectivity, say so explicitly and distinguish that from a genuine test failure.

Known-good baseline: **build with 0 warnings / 0 errors, 68 tests passed / 0 skipped, `dotnet format` clean.** Six projects build — Domain, Application, Infrastructure, Api, Reminders.Worker (`net10.0-windows`), and Tests. A drop in the test count or any new warning is a regression.

Architecture tests enforce the dependency direction. If one fails, the fix is the layering, not the test:

```text
ERSystem.Web.Api -> Application, Infrastructure
ERSystem.Web.Infrastructure -> Application, Domain
ERSystem.Web.Application -> Domain
ERSystem.Web.Domain -> nothing
```

## Cross-boundary changes

For a change that crosses the API and frontend boundary, also run `/verify-frontend` and verify both sides together: the request/response contract, role and report-level authorization, `ProblemDetails` error shapes, `409 Conflict` on stale or out-of-order workflow actions, row-version handling, and antiforgery on cookie-authenticated mutations.

For a change to a workflow or table shared with the desktop client, also trace the desktop call path and consider mixed-version deployment — the two interfaces are deployed independently.

## If a step cannot run

State the limitation plainly and continue with the steps that can run: a missing .NET 10 SDK, no network for restore, or no SQL Server for integration tests are all environment limitations to report, not successes.
