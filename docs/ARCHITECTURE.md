# ER System 3.0 Architecture

## Overview

ER System 3.0 is a legacy VB.NET Windows Forms expense reporting application targeting .NET Framework 4.8. The application should be modernized incrementally without a big-bang rewrite. The goal is to keep the app runnable while extracting business workflows, data access, configuration, and infrastructure concerns out of form code-behind files and legacy global modules.

## Current constraints

- Primary production project: `ER System/ER System.vbproj`
- Application type: VB.NET WinForms
- Target framework: .NET Framework 4.8
- Legacy modules and form code-behind still contain workflow and data access behavior
- Installer projects and SQL scripts are high-risk areas and should only change when explicitly required

## Target architecture

```text
ER System/
  Presentation/
    Forms/                 WinForms screens, designers, and UI-only logic
    Presenters/            UI orchestration that can be tested without controls when practical
    ViewModels/            Simple display/input models for forms and grids
  Application/
    UseCases/              Expense report workflows: create, file, approve, return, cancel, export
    Services/              Application services coordinating repositories and infrastructure
    Validation/            Business validation rules and user-facing validation messages
  Domain/
    Entities/              ExpenseReport, ExpenseLine, UserAccount, Department, Signature, Client
    Enums/                 File status, approval status, role/signatory IDs, report type values
    ValueObjects/          Money/rates, date ranges, registry connection settings, email addresses
  Infrastructure/
    Data/
      Repositories/        SQL Server repository implementations
      Sql/                 SQL command builders, stored procedure names, parameter helpers
    Configuration/         Registry/app.config settings adapters
    Email/                 SMTP/email sending adapters
    Reporting/             Crystal Reports and PDF/export adapters
    Logging/               log4net integration and error reporting adapters
  Shared/
    Extensions/            Small extension methods
    Utilities/             File/path helpers, encryption wrappers, common constants
  Resources/               Existing image/icon resources
  My Project/              Visual Studio-generated VB project files
```

## Dependency direction

Use this dependency direction for new code:

```text
Presentation -> Application -> Domain
Application -> Infrastructure abstractions only
Infrastructure -> Application/Domain contracts as needed
Shared -> no business workflow dependencies
```

Practical rule: a form may call an application service or presenter, but new services should not depend on WinForms controls.

## Architectural rules

1. Keep forms thin.
   - Forms should keep control events, visual state, and calls into services or presenters.
   - New SQL, registry, reporting, email, file-system, and workflow logic should not be added directly to forms.

2. Separate business workflows from infrastructure.
   - Workflow decisions belong under `Application/`.
   - SQL Server, registry, email, reporting, logging, and file-system integration belong under `Infrastructure/`.
   - Plain business models belong under `Domain/`.

3. Use repositories for database access.
   - New database access should go through repository classes.
   - Parameterize all values.
   - Prefer stored procedures where they already exist.
   - Wrap disposable ADO.NET objects in `Using` blocks.
   - Keep SQL/stored procedure constants centralized under `Infrastructure/Data/Sql/` when practical.

4. Treat legacy modules as compatibility seams.
   - Avoid adding new public global variables.
   - When touching a legacy module, prefer extracting a class or service and leaving only a small wrapper when needed.

5. Protect WinForms designer assets.
   - Do not manually edit `.Designer.vb` or `.resx` files unless the task explicitly requires it.
   - Move form files only in small, buildable units and preserve designer nesting metadata.

6. Improve safety incrementally.
   - Use `Option Strict On` in new VB files when practical.
   - Avoid empty `Catch` blocks.
   - Keep secrets in configuration and registry abstractions, not hard-coded in source.
   - Prefer named constants and enums over magic strings.

7. Preserve .NET Framework compatibility.
   - Do not introduce APIs that require .NET Core or modern .NET into this project.
   - Validate any new package against `net48` compatibility.

## Incremental roadmap

1. Document and stabilize.
   - Keep this document aligned with `.github/copilot-instructions.md`.
   - Focus first on the main workflows: login, create report, add expense, file report, approve or return report, and summary or export.
   - Maintain clear build instructions for a Windows developer environment.

2. Use the new folders without moving every file.
   - Add new code into architecture folders first.
   - Move legacy files only when a specific feature change justifies the move.

3. Extract configuration and connection management.
   - Wrap registry reads and writes behind configuration providers.
   - Centralize connection creation behind a factory or interface.

4. Extract data access by feature.
   - Start with one feature at a time, such as login or user account loading.
   - Introduce repository interfaces and SQL implementations.

5. Extract application services.
   - Move workflow decisions into application services.
   - Return simple result objects that forms can translate into messages.

6. Clean up forms gradually.
   - Keep controls and event handlers in forms.
   - Move validation, loading, state transitions, and save behavior into services.

7. Add tests around extracted logic.
   - Prefer tests for `Domain/` and `Application/` classes first.
   - Use repository abstractions to isolate business logic from database dependencies.

## Current implementation status

The project already contains initial examples of the target layering, including:

- `Application/Repositories/IUserAccountRepository.vb`
- `Application/Services/UserAccountService.vb`
- `Application/Services/LoginAccessService.vb`
- `Infrastructure/Configuration/RegistryConnectionSettingsProvider.vb`
- `Infrastructure/Data/Sql/SqlConnectionFactory.vb`
- `Infrastructure/Data/Repositories/SqlUserAccountRepository.vb`
- `Domain/Entities/UserAccount.vb`
- `Domain/Enums/UserLevel.vb`

These should be treated as the starting seam for continued low-risk refactoring.

## Non-goals for the current step

- No rewrite to another UI platform
- No mass renaming of forms
- No mass movement of designer files
- No database schema changes
- No installer project changes
- No Crystal Reports replacement
