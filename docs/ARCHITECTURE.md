# ER System 3.0 Architecture Plan

## Decision

Use an incremental **layered architecture with MVP-style WinForms seams** for ER System 3.0.

This is the best first architecture for this application because the codebase is a mature VB.NET Windows Forms system with many existing forms, designer files, global modules, SQL Server stored procedure calls, registry settings, Crystal Reports integration, and installer assets. A full rewrite or sudden migration to a different UI platform would create unnecessary risk. The safer path is to keep the working WinForms shell while extracting business workflows and infrastructure behind testable classes.

## Current baseline observed in the repository

- `ER System.sln` currently loads the `ER System/ER System.vbproj` WinForms application.
- The application targets .NET Framework 4.8.
- The VB project is form-heavy, with many `frm*.vb`, `.Designer.vb`, and `.resx` files in the project root.
- Shared behavior currently lives in global modules such as connection, loading, maintenance, reporting, reuse, and data-store modules.
- SQL access is spread through modules and forms, often by constructing command text directly.
- The application depends on Windows-specific technologies including WinForms, registry settings, Crystal Reports/ReportViewer-style reporting, and installer projects.

## Target folder structure

```text
ER System/
  Presentation/
    Forms/                 WinForms screens and UI-only event handling
    Presenters/            Form orchestration and presenter seams
    ViewModels/            Simple UI data models
  Application/
    UseCases/              Feature workflows, one user action per class where practical
    Services/              Application services coordinating repositories/infrastructure
    Validation/            Business validation rules and result messages
  Domain/
    Entities/              Plain business objects
    Enums/                 Statuses, roles, report types, signatory concepts
    ValueObjects/          Money/rates, date ranges, registry settings, email values
  Infrastructure/
    Data/
      Repositories/        SQL Server repository implementations
      Sql/                 Stored procedure names, SQL constants, parameter helpers
    Configuration/         Registry and app.config adapters
    Email/                 SMTP/email adapters
    Reporting/             Crystal Reports and export adapters
    Logging/               log4net/application logging adapters
  Shared/
    Extensions/            Small language/framework extensions
    Utilities/             File/path/encryption/common helpers
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

## Implemented architecture seams

The first real extraction is the database connection seam:

- `Infrastructure/Configuration/ConnectionSettings.vb` models registry-backed database settings.
- `Infrastructure/Configuration/RegistryConnectionSettingsProvider.vb` loads and decrypts existing registry settings.
- `Infrastructure/Data/Sql/SqlConnectionFactory.vb` creates SQL Server connections from those settings.
- `mConn.vb` remains as the legacy compatibility module, but now delegates connection-string creation to the infrastructure layer.
- `Domain/Entities/UserAccount.vb`, `Application/Repositories/IUserAccountRepository.vb`, `Application/Services/UserAccountService.vb`, and `Infrastructure/Data/Repositories/SqlUserAccountRepository.vb` provide the first repository/application-service slice for login and user status data access.

This implementation does **not** move form files, update designer nesting, change installer projects, or change user workflows. The touched user-account queries now use repository methods with SQL parameters.

## Refactoring sequence

1. **Create architecture folders and documentation**
   - Add layer folders with README placeholders.
   - Keep existing files in place until each move is justified and build-verified.

2. **Extract configuration and connection seams**
   - Introduce registry/app.config providers.
   - Introduce SQL connection factories.
   - Leave old module functions as compatibility wrappers where needed.

3. **Extract one repository at a time**
   - Start with a small feature such as user lookup, department lookup, or report list loading.
   - Replace string-concatenated commands with parameters.
   - Keep stored procedure behavior unchanged unless a database migration is explicitly requested.

4. **Extract one application workflow at a time**
   - Good candidates: login, file report, approve report, return/cancel report, export/print report.
   - Return result objects that forms can display without duplicating business rules.

5. **Move forms only after service seams exist**
   - Move `.vb`, `.Designer.vb`, and `.resx` together.
   - Update `.vbproj` entries and dependent metadata.
   - Verify in Visual Studio because WinForms designer behavior is Windows/VS-specific.

6. **Add tests around extracted code**
   - Test `Domain` and `Application` classes first.
   - Mock repository/configuration/email/reporting boundaries.

## Non-goals for the first architecture step

- No rewrite to web, WPF, MAUI, or .NET Core/.NET 5+.
- No mass renaming of forms.
- No mass movement of designer files.
- No database schema changes.
- No installer project changes.
- No Crystal Reports replacement.

## Review checklist for future architecture PRs

- Does the change keep the app buildable in Visual Studio?
- Are forms thinner after the change?
- Is database access parameterized?
- Are registry, email, reporting, and file-system calls isolated behind infrastructure classes?
- Did any form move include its `.Designer.vb` and `.resx` files?
- Are behavior changes documented separately from structure-only changes?
