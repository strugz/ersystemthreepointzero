# ER System 3.0 Agent Guide

## Purpose of This File

This is the canonical instruction file for AI coding agents and developers working in this repository. It explains what the application does, how the solution is organized, where new work belongs, and which safety rules must be followed.

Do not create a second `Agent.MD` or another competing root-level agent guide. Keep this file current when the solution structure or engineering rules materially change.

## What the Project Does

ER System 3.0 is an expense-reporting system with two production interfaces that operate against the same legacy SQL Server database:

- The legacy VB.NET Windows Forms application supports user login, creating and editing expense reports, recording expenses and attachments, filing reports, approval or return processing, finance tracking, summaries, exports, email, and reporting.
- The companion web portal under `Web/` currently supports Manager approval and Finance physical-receipt tracking while the desktop client remains active.

The desktop application targets .NET Framework 4.8 and depends on Windows-specific technologies, including WinForms and legacy configuration or registry behavior. The web portal uses an ASP.NET Core backend on .NET 10 and a Vue 3, TypeScript, Vuetify, and Vite frontend. The long-term goal is safer, incremental improvement without breaking established desktop workflows, web workflows, or compatibility between the two interfaces.

## Solution Overview

The repository has two intentionally separate application solutions. `ER System.sln` is the desktop solution and remains the solution to use for legacy WinForms work. `Web/Backend/ERSystem.Web.sln` is the web backend solution. The web frontend is managed independently through `Web/Frontend/ersystem-web-client/package.json`. Do not add the web projects to the desktop solution or installer unless an explicit deployment decision changes this boundary.

The active desktop projects and intended responsibilities are:

| Project | Type and responsibility |
| --- | --- |
| `ER System/ER System.vbproj` | WinForms executable. Owns application startup, forms, UI resources, and compatibility with legacy form and module code. |
| `ERSystem.Domain/ERSystem.Domain.vbproj` | Class library for entities, DTOs, approval concepts, value types, and business rules that do not depend on UI or infrastructure. |
| `ERSystem.Infrastructure/ERSystem.Infrastructure.vbproj` | Class library for database access, repositories, configuration, registry integration, and other external concerns. References Domain. |
| `ERSystem.AppServices/ERSystem.AppServices.vbproj` | Class library for application workflows and use-case coordination. References Domain and Infrastructure. |
| `ERSystem.Tests/ERSystem.Tests.vbproj` | Test library for focused unit, integration, and regression coverage. Currently references Domain and Infrastructure. |
| `ERSystem3.5Setup/ERSystem3.5Setup.vdproj` | Visual Studio deployment project used to build the Windows installer. |

The active web projects and intended responsibilities are:

| Project | Type and responsibility |
| --- | --- |
| `Web/Backend/src/ERSystem.Web.Api/ERSystem.Web.Api.csproj` | ASP.NET Core API host. Owns HTTP endpoints, authentication cookies, antiforgery, authorization policies, middleware, OpenAPI, health checks, and composition. |
| `Web/Backend/src/ERSystem.Web.Application/ERSystem.Web.Application.csproj` | Application contracts, DTOs, validation, pagination, and workflow coordination interfaces. References Web Domain. |
| `Web/Backend/src/ERSystem.Web.Domain/ERSystem.Web.Domain.csproj` | UI- and infrastructure-independent workflow rules and status values. Has no project dependencies. |
| `Web/Backend/src/ERSystem.Web.Infrastructure/ERSystem.Web.Infrastructure.csproj` | EF Core mappings, SQL Server access, legacy authentication compatibility, auditing, and service implementations. References Web Application and Web Domain. |
| `Web/Backend/src/ERSystem.Reminders.Worker/ERSystem.Reminders.Worker.csproj` | Unattended .NET 10 Windows Service for scheduled approval email and SMS gateway reminders. Reuses Web Application, Domain, and Infrastructure but is deployed independently from IIS. |
| `Web/Backend/tests/ERSystem.Web.Tests/ERSystem.Web.Tests.csproj` | Unit, architecture, and SQL Server integration tests for the web backend. |
| `Web/Frontend/ersystem-web-client/` | Vue 3 single-page application for Manager and Finance workflows, built and tested with Vite and Vitest. |

The WinForms executable references AppServices, Domain, and Infrastructure. Preserve this general dependency direction for incremental work:

```text
ER System (WinForms UI)
  -> ERSystem.AppServices
       -> ERSystem.Domain
       -> ERSystem.Infrastructure
            -> ERSystem.Domain

ERSystem.Tests
  -> tested project(s)
```

Avoid introducing dependencies from Domain back to Infrastructure, AppServices, or WinForms. Do not place WinForms control types in Domain, Infrastructure, or application-service interfaces.

Preserve the web backend dependency direction:

```text
ERSystem.Web.Api
  -> ERSystem.Web.Application
  -> ERSystem.Web.Infrastructure

ERSystem.Web.Infrastructure
  -> ERSystem.Web.Application
       -> ERSystem.Web.Domain
  -> ERSystem.Web.Domain

ERSystem.Web.Domain
  -> nothing
```

The desktop and web codebases share database contracts and workflow behavior, not direct project references. Do not make either application depend on the other application's UI, host, or infrastructure assemblies. Coordinate shared workflow changes through compatible database changes and equivalent business rules until a deliberately shared, framework-compatible contract is introduced.

## Repository Map

### Authoritative solution areas

- `ER System/` contains the production WinForms executable. It includes current presentation code, resources, configuration, compatibility layers, and legacy code that has not yet been extracted.
- `ERSystem.Domain/` contains the separate domain library. Existing areas include `Approval/`, `Dtos/`, and `Entities/`.
- `ERSystem.AppServices/` contains application-level services and workflow coordination under `Services/`.
- `ERSystem.Infrastructure/` contains configuration, data-access, and repository implementations under `Configuration/`, `Data/`, and `Repositories/`.
- `ERSystem.Tests/` contains automated tests organized to mirror the area under test. `Infrastructure/` currently contains the available database-context tests.
- `Web/` contains the companion web portal. `Web/Backend/` contains the independent ASP.NET Core solution, `Web/Frontend/ersystem-web-client/` contains the Vue single-page application, `Web/README.md` contains setup and deployment instructions, and `Web/AGENTS.md` contains rules specific to work under `Web/`.
- `Database/` contains ordered SQL migration scripts. Add schema changes here as new, dated scripts; do not silently rewrite migration history that may already have been applied.
- `ERSystem3.5Setup/` contains the installer project and its generated installer outputs.

### Web application subfolders

- `Web/Backend/src/ERSystem.Web.Api/` owns the HTTP boundary and application composition. Keep controllers and middleware focused on transport, authentication, authorization, validation, and response mapping.
- `Web/Backend/src/ERSystem.Web.Application/` owns feature contracts and application abstractions. It must remain independent of ASP.NET Core hosting and EF Core persistence details.
- `Web/Backend/src/ERSystem.Web.Domain/` owns pure workflow rules and values. It must not reference Application, Infrastructure, API, Frontend, or desktop code.
- `Web/Backend/src/ERSystem.Web.Infrastructure/` owns persistence, legacy database compatibility, audit writing, authentication compatibility, and external integrations.
- `Web/Backend/src/ERSystem.Reminders.Worker/` owns only Windows Service hosting and daily scheduling; reminder rules, messages, persistence, SMTP, and `dbo.sp_Notify` integration remain in their owning shared layers.
- `Web/Backend/tests/ERSystem.Web.Tests/` mirrors backend behavior with unit, architecture, and integration coverage.
- `Web/Frontend/ersystem-web-client/src/features/` owns feature-specific API modules, types, and UI; `src/views/` coordinates route-level screens; `src/layouts/` owns application shells; `src/app/` owns routing, plugins, and global stores; and `src/shared/` owns dependency-light reusable UI, API, formatting, validation, and design primitives.

For any file under `Web/`, follow `Web/AGENTS.md` in addition to this root guide. The root guide remains authoritative for repository-wide rules and shared desktop/web behavior.

### Production application subfolders

The `ER System/` project is in a transitional state and contains both legacy files and newer architectural folders:

- `Presentation/` is the preferred production location for WinForms presentation code, including forms, presenters, and view models when those patterns are introduced incrementally.
- `Application/`, `Domain/`, `Infrastructure/`, and `AppServices/` contain work that was historically or incrementally organized inside the executable project. Before adding a file there, check whether it belongs in the corresponding separate solution project instead.
- `Legacy/` and root-level forms or modules are compatibility areas. Modify them only when required; do not use them as the default destination for new design.
- `Shared/` is for narrowly reusable helpers with no feature-specific business workflow. Do not turn it into a miscellaneous dumping ground.
- `Resources/`, `My Project/`, `Packages/`, `app.config`, and `packages.config` support the executable, its generated settings/resources, and NuGet dependencies.
- `publish/` and build-output folders are generated or deployment artifacts, not locations for source code.

### Supporting and historical areas

- `README.md` is the desktop improvement roadmap and target architecture reference. Its proposed folders describe direction, not proof that migration is complete. Use `Web/README.md` for the web portal's setup, database preparation, local development, build, and deployment guidance.
- `ERF_UPDATE_PLAN.md` and the root SQL/schema reference text files provide planning or database-discovery context. Verify them against current code and database scripts before treating them as authoritative runtime behavior.
- `Presentation/` at the repository root is a supporting or historical area outside the active WinForms project. Prefer the presentation structure included in an active `.vbproj` unless a task explicitly establishes another purpose.
- `Instructions/` contains user or setup screenshots and is supporting documentation, not application source.
- `tools/` is reserved for repository maintenance or developer tooling. It currently contains the report-only web clean-architecture audit, its optional Windows scheduled-task registration script, and the scanned-receipt integrity verifier. Do not put runtime application behavior there.
- `.github/` contains repository-level GitHub and coding-assistant configuration.
- `packages/` is the restored legacy NuGet package directory. Do not hand-edit package contents.
- `ERMSystem/`, `ERMSystem3.0/`, and `JFramework/` are historical or supporting code areas and are not active projects in `ER System.sln`. Do not migrate code into or out of them without an explicit, verified requirement.
- `.vs/`, `bin/`, `obj/`, `TestResults/`, `node_modules/`, `dist/`, `coverage/`, setup `Debug/` or `Release/`, and similar output directories are generated artifacts. Do not treat them as source or include incidental changes from them.

If the purpose of an unfamiliar directory is unclear, inspect its project inclusion and call sites before modifying it. Being present in the repository does not make a folder part of the active application.

## Important Files and Entry Points

- `ER System.sln` is the solution to use for desktop builds and desktop project relationship checks.
- `Web/Backend/ERSystem.Web.sln` is the solution to use for web API builds, tests, formatting, and backend project relationship checks.
- `Web/Frontend/ersystem-web-client/package.json` defines the supported frontend runtime, dependencies, and development, lint, type-check, test, and build commands. Keep `package-lock.json` synchronized with intentional dependency changes.
- `Web/Backend/src/ERSystem.Web.Api/Program.cs` is the web API composition and startup entry point. `Web/Frontend/ersystem-web-client/src/main.ts` and `src/app/router/index.ts` are the frontend startup and route entry points.
- `Web/Backend/src/ERSystem.Web.Api/appsettings.json` contains non-secret web defaults. Supply connection strings, encryption keys, and deployment secrets through user secrets or protected environment/IIS configuration.
- `ER System/ER System.vbproj` defines the executable, its startup object, source inclusion, references, resources, and build settings.
- `ER System/My Project/Application.myapp` and the generated application files under `My Project/` define WinForms application startup behavior. Treat generated files carefully.
- `ER System/app.config` contains executable configuration. `ERSystem.Infrastructure/App.config` and `ERSystem.Tests/App.config` contain project-specific configuration used by those assemblies or tests.
- Each `packages.config` records legacy NuGet dependencies for its project.
- `Database/*.sql` contains incremental database changes shared by the desktop and web applications. The large root SQL and schema-reference files, including `ER3.0.sql`, are live-schema reference material, not migrations and not substitutes for a reviewed forward script.
- WinForms behavior is primarily in `.vb` form code-behind paired with `.Designer.vb` and `.resx` files. Preserve all three files and their project metadata when working with forms.
- Important legacy compatibility modules include `ER System/mConn.vb`, `modLoadingData.vb`, `modMaintenance.vb`, `modReport.vb`, `modReuse.vb`, and `ModDataStore.vb`. Confirm their current locations and project inclusion before editing because the codebase is being reorganized gradually.
- High-risk workflow forms include login, expense-report creation and editing, the main window, filing, approval, return, cancellation, summaries, and exports. Trace the current call path before changing these workflows.

## Where New Files Belong

Choose the smallest active project that owns the responsibility. First determine whether the behavior belongs to the desktop application, the web portal, or a shared database contract. Prefer the separate solution projects over creating duplicate layers inside the WinForms executable.

| New responsibility | Preferred location |
| --- | --- |
| Form, dialog, control-binding logic, presenter, or UI view model | `ER System/Presentation/` and the appropriate subfolder included by `ER System.vbproj` |
| Application use case, workflow orchestration, or service coordinating repositories and domain rules | `ERSystem.AppServices/Services/` or a focused feature subfolder in that project |
| Entity, DTO, enum, value object, approval rule, or UI-independent business rule | A focused folder in `ERSystem.Domain/` |
| SQL execution, connection creation, repository implementation, registry/configuration access, email, reporting, file system, or other external integration | A focused folder in `ERSystem.Infrastructure/` |
| Unit, integration, or regression test | `ERSystem.Tests/`, mirroring the production namespace or feature |
| Web route, controller, authentication/authorization policy, middleware, OpenAPI, health check, or HTTP response mapping | `Web/Backend/src/ERSystem.Web.Api/` |
| Web DTO, use-case contract, validation, pagination, or orchestration interface | A focused feature folder in `Web/Backend/src/ERSystem.Web.Application/` |
| Pure web workflow rule, status, or value type | A focused folder in `Web/Backend/src/ERSystem.Web.Domain/` |
| EF Core mapping, web repository/service implementation, audit persistence, legacy authentication compatibility, or web external integration | A focused folder in `Web/Backend/src/ERSystem.Web.Infrastructure/` |
| Web backend unit, architecture, or integration test | `Web/Backend/tests/ERSystem.Web.Tests/`, mirroring the production area |
| Unattended reminder host or Windows Service scheduling | `Web/Backend/src/ERSystem.Reminders.Worker/`; keep business and integration logic in Domain, Application, or Infrastructure |
| Web feature component, typed API module, or feature-specific type | `Web/Frontend/ersystem-web-client/src/features/<feature>/` |
| Web route-level view, layout, application setup, or genuinely reusable UI/composable | The matching `views/`, `layouts/`, `app/`, or `shared/` folder under `Web/Frontend/ersystem-web-client/src/` |
| Database schema or stored-procedure change | A new dated and descriptively named script in `Database/` |
| General developer or repository automation | `tools/`, only when it is not runtime application code |

Add new `.vb` files to the correct legacy `.vbproj`; these projects do not use modern SDK-style wildcard inclusion. Verify build action, root namespace behavior, project references, and form metadata as applicable.

Web backend projects use SDK-style default file inclusion. Keep new C# files in the owning layer without adding redundant compile entries. For frontend work, use Vue 3 Composition API with `<script setup lang="ts">`, typed props and events, and the existing feature-first import boundaries defined in `Web/AGENTS.md`.

Do not interpret the target structure in `README.md` as permission to create every proposed folder, move many existing files, or perform a broad architectural rewrite. Introduce only the folder and class needed for the current focused change.

## Working Style

- Make small, buildable changes.
- Preserve existing behavior unless the task explicitly asks for a behavior change.
- Avoid big-bang rewrites, broad file moves, and large structural changes.
- Move one focused responsibility at a time and verify after each meaningful change.
- Prefer existing project patterns unless they conflict with the improvement direction in `README.md` or the dependency rules in this guide.
- Keep changes scoped to the requested task.
- Do not clean up unrelated files, reformat unrelated legacy code, or revert user changes.
- Inspect call sites, designer relationships, project inclusion, configuration, and database dependencies before changing legacy workflows.
- When changing a workflow or table used by both interfaces, trace both the desktop and web call paths, authorization rules, status mappings, concurrency behavior, and deployment order before editing.
- Preserve backward compatibility while desktop and web versions may be deployed independently. Do not assume every client and database will be upgraded at the same time.
- Treat legacy modules and form code as compatibility boundaries, not as places for new architecture.

## Architecture and Coding Rules

### Forms and application logic

- Keep forms focused on reading and validating control input, calling services or presenters, binding results, and displaying messages or visual state.
- Keep event handlers thin. Extract workflow decisions, reusable validation, data access, configuration, email, and reporting logic into focused collaborators.
- Put new business logic into application services, use-case classes, or Domain types as appropriate.
- Prefer typed inputs and return values with explicit dependencies over shared mutable state or collaborators that directly manipulate controls.
- Avoid adding new mutable `Public` state to modules. When legacy shared state must remain, contain access behind the narrowest compatibility seam possible.

### VB.NET and WinForms

- Keep .NET Framework 4.8 compatibility; do not use APIs or language features unsupported by the configured compiler and target framework.
- Use `Option Strict On` in new VB files when practical and retain `Option Explicit On` behavior.
- Use clear, descriptive names for classes, methods, variables, and controls.
- Prefer classes over modules for new behavior.
- Do not manually edit `.Designer.vb` files unless the task explicitly requires it and the designer relationship has been understood.
- Preserve form names, partial-class relationships, `.resx` resources, `DependentUpon` metadata, and designer compatibility when moving or renaming files.
- Do not add new SQL, registry, email, reporting, or business workflow logic directly to form code-behind.

### ASP.NET Core web backend

- Keep controllers thin. They should validate the HTTP request, call application services, enforce policies, and map results; workflow rules and business SQL belong outside controllers.
- Use request and response DTOs. Do not expose EF Core entities or accept client-supplied user IDs as authorization evidence.
- Prefix API routes with `/api`, use asynchronous operations with `CancellationToken`, return RFC `ProblemDetails` for errors, and use `409 Conflict` for stale, duplicate, or out-of-order workflow actions.
- Enforce role and report-level authorization in the API. Frontend permission checks are presentation behavior only.
- Protect cookie-authenticated mutations with antiforgery validation. Preserve Secure, HttpOnly, and appropriate SameSite cookie behavior in deployment.
- Use the existing centralized pagination, row-version, current-user, audit, transaction, and exception-mapping patterns. Do not introduce generic repositories, base controllers, MediatR, or catch-all helpers without an explicit architectural requirement.

### Vue, TypeScript, and Vuetify frontend

- Use Vue 3 Composition API with `<script setup lang="ts">` and preserve the `app -> views/layouts -> features -> shared` dependency direction.
- `shared` must not import from feature, view, layout, or app code. Features must not import directly from other features; route views coordinate cross-feature behavior.
- Keep API access in typed feature API modules or the shared API client. Do not put HTTP calls, business SQL, or workflow decisions in templates and presentational components.
- Use Pinia for session identity, permissions, and truly cross-route state. Keep form, dialog, filtering, and queue state local unless sharing is demonstrably required.
- Search existing components, composables, tokens, validation rules, formatters, status mappings, pagination, dialogs, and notification infrastructure before adding another implementation.
- Reusable UI must accept typed props, emit typed events, expose loading, disabled, empty, permission, and error states where applicable, use design tokens, and remain keyboard-accessible and responsive.

### Database and configuration

- Use `Using` blocks for connections, commands, readers, adapters, streams, and other disposable resources.
- Use `SqlCommand.Parameters` for all input values. Do not concatenate user-controlled or variable input into SQL commands.
- Prefer stored procedures where the existing database already supports them.
- Centralize stored-procedure names, SQL constants, registry paths, and setting keys where practical for the focused change.
- Put new data access behind repository or infrastructure classes.
- Keep credentials and secrets out of form code, source files, logs, and committed configuration.
- Wrap registry and connection-string access in dedicated configuration classes.
- `LegacyErDbContext` maps legacy tables and does not own migrations. `WebWorkflowDbContext` owns only web security and audit tables. Do not let the web application claim migration ownership over the legacy schema.
- Keep the ER database at SQL Server compatibility level 100 unless an explicit, tested upgrade plan changes it. Verify EF Core-generated queries and new SQL against the deployed SQL Server version and compatibility level.
- Use transactions for approval and Finance mutations, concurrency tokens for records shared by desktop and web clients, and durable audit events for security-sensitive workflow changes.
- Do not run migrations automatically against production or execute `ER3.0.sql` wholesale. For database changes, provide reviewed, dated forward and appropriate rollback scripts and consider compatibility with deployed clients and partially upgraded environments.
- Restrict server-side file and receipt access to configured, allowlisted storage roots. Never trust or delete an arbitrary client-supplied path.

### Error handling and logging

- Do not add empty `Catch` blocks or silently discard failures.
- Preserve useful exception context when wrapping or reporting errors.
- Log, return, or surface meaningful errors according to the existing workflow and user experience.
- Avoid abrupt termination patterns such as `End`; prefer controlled shutdown or a clear failure result.
- Do not expose credentials, encrypted passwords, cookies, antiforgery tokens, connection strings, encryption keys, receipt contents, personal data, or sensitive report content in exception messages, audit payloads, or logs.

### Comments

- Add concise comments for complex business rules, legacy compatibility decisions, database or configuration behavior, cross-interface concurrency, and non-obvious WinForms or web workflow constraints.
- Explain why the code exists rather than narrating each line.
- Avoid obvious comments that repeat the implementation.
- Preserve existing comments unless they are clearly incorrect or misleading; update them when behavior changes make them stale.

## Build and Verification

- Use Visual Studio or compatible MSBuild tooling that supports .NET Framework 4.8, VB.NET legacy project files, and the Visual Studio deployment project when installer validation is required.
- Build `ER System.sln` after meaningful code or project-file changes when the local environment supports it. Prefer the configuration and platform relevant to the task, commonly Debug with Any CPU or x86.
- Run focused tests from `ERSystem.Tests` when changing test-covered domain, infrastructure, data-access, or business behavior. Add regression coverage when a bug can be isolated without depending on WinForms controls.
- For web backend work, use the .NET SDK version required by `Web/Backend/Directory.Build.props`. From `Web/Backend`, run `dotnet restore ERSystem.Web.sln`, `dotnet build ERSystem.Web.sln`, `dotnet test ERSystem.Web.sln`, and `dotnet format ERSystem.Web.sln --verify-no-changes` as appropriate.
- For web frontend work, use Node.js 20.19 or later. From `Web/Frontend/ersystem-web-client`, run `npm ci`, `npm run lint`, `npm run type-check`, `npm run test`, and `npm run build` as appropriate. Use `npm ci`, not an untracked dependency installation, for reproducible verification.
- After restoring the web backend and frontend dependencies, run the report-only clean-architecture audit from the repository root with `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Invoke-WebCleanArchitectureAudit.ps1`. Reports are written outside the repository under `%LOCALAPPDATA%\ERSystem\CleanArchitectureAudits`.
- Register the optional daily audit only when explicitly requested because it modifies Windows Task Scheduler: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Register-WebCleanArchitectureAudit.ps1`. The script accepts optional `-TaskName <name>` and `-DailyTime <HH:mm>` parameters.
- Verify scanned-receipt database integrity and local file inventory with `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Verify-ScannedReceipts.ps1 -ConfigPath <app.config> -ReceiptsRoot <directory> [-ErfReferenceNo <reference>]`. This is a read-only check and exits nonzero when database integrity violations are found.
- When a change crosses the web API and frontend boundary, verify both sides and check the request/response contract, permissions, error states, row-version handling, and antiforgery behavior together.
- For database work, validate script ordering, repeatability expectations, object names, parameter types, and compatibility with the application query or repository that consumes the change.
- For shared database workflow work, verify both desktop and web consumers, SQL Server compatibility level 100, transaction boundaries, concurrency conflicts, audit behavior, and mixed-version deployment compatibility.
- For form work, verify the form still opens in the designer when possible and exercise the changed workflow without modifying generated files unnecessarily.
- For web UI work, exercise loading, empty, error, permission-denied, stale-data, keyboard, and responsive states for the affected workflow.
- For installer changes, build or inspect `ERSystem3.5Setup` separately because deployment-project support may not be available in every MSBuild environment.
- The web portal is deployed separately from `ERSystem3.5Setup`. Follow `Web/README.md` for the IIS publish layout and same-origin SPA/API deployment requirements.
- Documentation-only changes do not require runtime tests or a solution build. Check paths, Markdown rendering, and consistency with both solution files, `README.md`, `Web/README.md`, and `Web/AGENTS.md`.
- If a build or test cannot run because of missing Windows, Visual Studio, .NET SDK, Node.js, SQL Server, registry, reporting, IIS, or other environment dependencies, state the limitation clearly rather than claiming success.
- Keep verification proportional to the risk and blast radius of the change.

## Do Not Do

- Do not rewrite the application from scratch.
- Do not move many forms, modules, projects, or source files at once.
- Do not merge the desktop and web solutions, domain layers, infrastructure layers, build pipelines, or deployment packages merely because they share a database.
- Do not create proposed architecture folders merely to make the tree resemble `README.md`.
- Do not casually change generated designer, resource, settings, or installer-output files.
- Do not introduce new global mutable state.
- Do not place new business rules or data access in WinForms code-behind, Vue components, or API controllers.
- Do not add dependencies that reverse the intended project direction.
- Do not replace established workflow behavior without an explicit requirement.
- Do not rely on frontend permissions for security, trust client-supplied identities, expose EF entities from endpoints, or omit antiforgery protection from cookie-authenticated mutations.
- Do not duplicate API clients, dialog frameworks, pagination implementations, status mappings, notification systems, or cross-cutting backend services already present under `Web/`.
- Do not automatically apply production database migrations, rewrite applied migration history, or use the live-schema reference as a migration.
- Do not make unrelated formatting, cleanup, package, output, or project-file churn.
- Do not hide errors with silent catches.
- Do not commit secrets, machine-specific configuration, generated build artifacts, or restored package contents.
