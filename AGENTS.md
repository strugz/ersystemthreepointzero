# ER System 3.0 Agent Guide

## Purpose of This File

This is the canonical instruction file for AI coding agents and developers working in this repository. It explains what the application does, how the solution is organized, where new work belongs, and which safety rules must be followed.

Do not create a second `Agent.MD` or another competing root-level agent guide. Keep this file current when the solution structure or engineering rules materially change.

## What the Project Does

ER System 3.0 is a legacy VB.NET Windows Forms expense-reporting application. It supports operational workflows such as user login, creating and editing expense reports, recording expenses and attachments, filing reports, approval or return processing, finance tracking, summaries, exports, email, and reporting.

The application targets .NET Framework 4.8 and depends on Windows-specific technologies, including WinForms and legacy configuration or registry behavior. It is a form-heavy application with historical global modules and shared state. The long-term goal is safer, incremental improvement without breaking the existing desktop application or its established workflows.

## Solution Overview

The main solution is `ER System.sln`. Its active projects and intended responsibilities are:

| Project | Type and responsibility |
| --- | --- |
| `ER System/ER System.vbproj` | WinForms executable. Owns application startup, forms, UI resources, and compatibility with legacy form and module code. |
| `ERSystem.Domain/ERSystem.Domain.vbproj` | Class library for entities, DTOs, approval concepts, value types, and business rules that do not depend on UI or infrastructure. |
| `ERSystem.Infrastructure/ERSystem.Infrastructure.vbproj` | Class library for database access, repositories, configuration, registry integration, and other external concerns. References Domain. |
| `ERSystem.AppServices/ERSystem.AppServices.vbproj` | Class library for application workflows and use-case coordination. References Domain and Infrastructure. |
| `ERSystem.Tests/ERSystem.Tests.vbproj` | Test library for focused unit, integration, and regression coverage. Currently references Domain and Infrastructure. |
| `ERSystem3.5Setup/ERSystem3.5Setup.vdproj` | Visual Studio deployment project used to build the Windows installer. |

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

## Repository Map

### Authoritative solution areas

- `ER System/` contains the production WinForms executable. It includes current presentation code, resources, configuration, compatibility layers, and legacy code that has not yet been extracted.
- `ERSystem.Domain/` contains the separate domain library. Existing areas include `Approval/`, `Dtos/`, and `Entities/`.
- `ERSystem.AppServices/` contains application-level services and workflow coordination under `Services/`.
- `ERSystem.Infrastructure/` contains configuration, data-access, and repository implementations under `Configuration/`, `Data/`, and `Repositories/`.
- `ERSystem.Tests/` contains automated tests organized to mirror the area under test. `Infrastructure/` currently contains the available database-context tests.
- `Database/` contains ordered SQL migration scripts. Add schema changes here as new, dated scripts; do not silently rewrite migration history that may already have been applied.
- `ERSystem3.5Setup/` contains the installer project and its generated installer outputs.

### Production application subfolders

The `ER System/` project is in a transitional state and contains both legacy files and newer architectural folders:

- `Presentation/` is the preferred production location for WinForms presentation code, including forms, presenters, and view models when those patterns are introduced incrementally.
- `Application/`, `Domain/`, `Infrastructure/`, and `AppServices/` contain work that was historically or incrementally organized inside the executable project. Before adding a file there, check whether it belongs in the corresponding separate solution project instead.
- `Legacy/` and root-level forms or modules are compatibility areas. Modify them only when required; do not use them as the default destination for new design.
- `Shared/` is for narrowly reusable helpers with no feature-specific business workflow. Do not turn it into a miscellaneous dumping ground.
- `Resources/`, `My Project/`, `Packages/`, `app.config`, and `packages.config` support the executable, its generated settings/resources, and NuGet dependencies.
- `publish/` and build-output folders are generated or deployment artifacts, not locations for source code.

### Supporting and historical areas

- `README.md` is the improvement roadmap and target architecture reference. Its proposed folders describe direction, not proof that migration is complete.
- `ERF_UPDATE_PLAN.md` and the root SQL/schema reference text files provide planning or database-discovery context. Verify them against current code and database scripts before treating them as authoritative runtime behavior.
- `Presentation/` at the repository root is a supporting or historical area outside the active WinForms project. Prefer the presentation structure included in an active `.vbproj` unless a task explicitly establishes another purpose.
- `Instructions/` contains user or setup screenshots and is supporting documentation, not application source.
- `tools/` is reserved for repository maintenance or developer tooling. Do not put runtime application behavior there.
- `.github/` contains repository-level GitHub and coding-assistant configuration.
- `packages/` is the restored legacy NuGet package directory. Do not hand-edit package contents.
- `ERMSystem/`, `ERMSystem3.0/`, and `JFramework/` are historical or supporting code areas and are not active projects in `ER System.sln`. Do not migrate code into or out of them without an explicit, verified requirement.
- `.vs/`, `bin/`, `obj/`, `TestResults/`, setup `Debug/` or `Release/`, and similar output directories are generated artifacts. Do not treat them as source or include incidental changes from them.

If the purpose of an unfamiliar directory is unclear, inspect its project inclusion and call sites before modifying it. Being present in the repository does not make a folder part of the active application.

## Important Files and Entry Points

- `ER System.sln` is the solution to use for normal builds and project relationship checks.
- `ER System/ER System.vbproj` defines the executable, its startup object, source inclusion, references, resources, and build settings.
- `ER System/My Project/Application.myapp` and the generated application files under `My Project/` define WinForms application startup behavior. Treat generated files carefully.
- `ER System/app.config` contains executable configuration. `ERSystem.Infrastructure/App.config` and `ERSystem.Tests/App.config` contain project-specific configuration used by those assemblies or tests.
- Each `packages.config` records legacy NuGet dependencies for its project.
- `Database/*.sql` contains incremental database changes. The large root SQL and schema-reference files are reference material, not substitutes for a reviewed migration.
- WinForms behavior is primarily in `.vb` form code-behind paired with `.Designer.vb` and `.resx` files. Preserve all three files and their project metadata when working with forms.
- Important legacy compatibility modules include `ER System/mConn.vb`, `modLoadingData.vb`, `modMaintenance.vb`, `modReport.vb`, `modReuse.vb`, and `ModDataStore.vb`. Confirm their current locations and project inclusion before editing because the codebase is being reorganized gradually.
- High-risk workflow forms include login, expense-report creation and editing, the main window, filing, approval, return, cancellation, summaries, and exports. Trace the current call path before changing these workflows.

## Where New Files Belong

Choose the smallest active project that owns the responsibility. Prefer the separate solution projects over creating duplicate layers inside the WinForms executable.

| New responsibility | Preferred location |
| --- | --- |
| Form, dialog, control-binding logic, presenter, or UI view model | `ER System/Presentation/` and the appropriate subfolder included by `ER System.vbproj` |
| Application use case, workflow orchestration, or service coordinating repositories and domain rules | `ERSystem.AppServices/Services/` or a focused feature subfolder in that project |
| Entity, DTO, enum, value object, approval rule, or UI-independent business rule | A focused folder in `ERSystem.Domain/` |
| SQL execution, connection creation, repository implementation, registry/configuration access, email, reporting, file system, or other external integration | A focused folder in `ERSystem.Infrastructure/` |
| Unit, integration, or regression test | `ERSystem.Tests/`, mirroring the production namespace or feature |
| Database schema or stored-procedure change | A new dated and descriptively named script in `Database/` |
| General developer or repository automation | `tools/`, only when it is not runtime application code |

Add new `.vb` files to the correct legacy `.vbproj`; these projects do not use modern SDK-style wildcard inclusion. Verify build action, root namespace behavior, project references, and form metadata as applicable.

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

### Database and configuration

- Use `Using` blocks for connections, commands, readers, adapters, streams, and other disposable resources.
- Use `SqlCommand.Parameters` for all input values. Do not concatenate user-controlled or variable input into SQL commands.
- Prefer stored procedures where the existing database already supports them.
- Centralize stored-procedure names, SQL constants, registry paths, and setting keys where practical for the focused change.
- Put new data access behind repository or infrastructure classes.
- Keep credentials and secrets out of form code, source files, logs, and committed configuration.
- Wrap registry and connection-string access in dedicated configuration classes.
- For database changes, provide a forward migration script and consider compatibility with deployed clients and partially upgraded environments.

### Error handling and logging

- Do not add empty `Catch` blocks or silently discard failures.
- Preserve useful exception context when wrapping or reporting errors.
- Log, return, or surface meaningful errors according to the existing workflow and user experience.
- Avoid abrupt termination patterns such as `End`; prefer controlled shutdown or a clear failure result.
- Do not expose credentials, connection strings, personal data, or sensitive report content in exception messages or logs.

### Comments

- Add concise comments for complex business rules, legacy compatibility decisions, database or configuration behavior, and non-obvious WinForms workflow constraints.
- Explain why the code exists rather than narrating each line.
- Avoid obvious comments that repeat the implementation.
- Preserve existing comments unless they are clearly incorrect or misleading; update them when behavior changes make them stale.

## Build and Verification

- Use Visual Studio or compatible MSBuild tooling that supports .NET Framework 4.8, VB.NET legacy project files, and the Visual Studio deployment project when installer validation is required.
- Build `ER System.sln` after meaningful code or project-file changes when the local environment supports it. Prefer the configuration and platform relevant to the task, commonly Debug with Any CPU or x86.
- Run focused tests from `ERSystem.Tests` when changing test-covered domain, infrastructure, data-access, or business behavior. Add regression coverage when a bug can be isolated without depending on WinForms controls.
- For database work, validate script ordering, repeatability expectations, object names, parameter types, and compatibility with the application query or repository that consumes the change.
- For form work, verify the form still opens in the designer when possible and exercise the changed workflow without modifying generated files unnecessarily.
- For installer changes, build or inspect `ERSystem3.5Setup` separately because deployment-project support may not be available in every MSBuild environment.
- Documentation-only changes do not require runtime tests or a solution build. Check paths, Markdown rendering, and consistency with the solution and `README.md`.
- If a build or test cannot run because of missing Windows, Visual Studio, database, registry, reporting, or other environment dependencies, state the limitation clearly rather than claiming success.
- Keep verification proportional to the risk and blast radius of the change.

## Do Not Do

- Do not rewrite the application from scratch.
- Do not move many forms, modules, projects, or source files at once.
- Do not create proposed architecture folders merely to make the tree resemble `README.md`.
- Do not casually change generated designer, resource, settings, or installer-output files.
- Do not introduce new global mutable state.
- Do not place new business rules or data access in WinForms code-behind.
- Do not add dependencies that reverse the intended project direction.
- Do not replace established workflow behavior without an explicit requirement.
- Do not make unrelated formatting, cleanup, package, output, or project-file churn.
- Do not hide errors with silent catches.
- Do not commit secrets, machine-specific configuration, generated build artifacts, or restored package contents.
