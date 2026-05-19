# ER System 3.0 Improvement Plan

## Purpose

This README is the working reference for improving the ER System 3.0 codebase in a safe, incremental way.

The application is a legacy VB.NET Windows Forms project targeting .NET Framework 4.8. The goal is to improve structure, maintainability, and safety without a big-bang rewrite and without breaking the existing WinForms application.

This improvement plan also introduces a proposed folder structure so files can be organized more clearly as the project is refactored over time.

## Current Project Snapshot

- Solution: `ER System.sln`
- Main app project: `ER System/ER System.vbproj`
- Application type: VB.NET WinForms
- Target framework: `.NET Framework 4.8`
- Current style: form-heavy code-behind with significant use of global modules
- Important legacy areas:
  - `ER System/mConn.vb`
  - `ER System/modLoadingData.vb`
  - `ER System/modMaintenance.vb`
  - `ER System/modReport.vb`
  - `ER System/modReuse.vb`
  - `ER System/ModDataStore.vb`

## Main Problems Observed

### 1. Forms contain too much logic
Forms currently handle UI events, workflow rules, database access, registry access, file handling, and validation in the same code-behind files.

Examples:
- `ER System/frmLogin.vb`
- `ER System/frmEReport.vb`
- `ER System/frmMain.vb`

### 2. Global modules hold shared state
Several modules expose many `Public` variables and shared procedures. This makes the application harder to reason about, test, and change safely.

Examples:
- `mConn.vb`
- `modLoadingData.vb`

### 3. Database access is mixed with UI code
Some database logic is executed directly from forms and modules. In some places SQL statements are built with string concatenation instead of parameters.

### 4. Error handling needs improvement
The codebase contains empty `Catch` blocks and abrupt shutdown patterns such as `End`.

### 5. Repeated magic values
Registry paths, connection settings, encryption usage, hard-coded strings, and workflow values are repeated across files.

### 6. Large methods are difficult to maintain
Some event handlers and helper methods are too large and perform multiple responsibilities.

## Improvement Goals

1. Keep the application runnable throughout the refactoring.
2. Improve code safety without changing behavior unless required.
3. Move business and data logic out of forms.
4. Reduce reliance on global mutable state.
5. Create testable seams around workflows and data access.
6. Preserve WinForms designer stability.
7. Keep .NET Framework 4.8 compatibility.

## Guiding Principles

- Do not rewrite the entire project at once.
- Do not move every file at once.
- Keep forms focused on UI concerns.
- Put new business logic into application services.
- Put new database and registry code into infrastructure classes.
- Treat legacy modules as compatibility seams, not as places for new design.
- Prefer small, buildable changes.
- Avoid manual edits to `.Designer.vb` files unless the task explicitly requires it.

## Target Direction

Use the following structure gradually. New code should prefer these folders even if legacy files remain in place for now.

```text
ER System/
  Presentation/
    Forms/
    Presenters/
    ViewModels/
  Application/
    UseCases/
    Services/
    Validation/
  Domain/
    Entities/
    Enums/
    ValueObjects/
  Infrastructure/
    Data/
      Repositories/
      Sql/
    Configuration/
    Email/
    Reporting/
    Logging/
  Shared/
    Extensions/
    Utilities/
```

## Coding Rules for New and Refactored Work

### General VB.NET rules
- Use `Option Strict On` in new VB files whenever practical.
- Keep `Option Explicit On` behavior.
- Use clear, descriptive names.
- Avoid adding new `Public` global variables in modules.
- Prefer classes over modules for new behavior.
- Prefer returning typed results over setting shared state.

### WinForms rules
- Keep forms thin.
- Forms should:
  - read input from controls
  - call services or presenters
  - display results/messages
  - update visual state
- Forms should not become the place for new SQL, registry, email, reporting, or business workflow logic.
- Do not manually edit `.Designer.vb` unless explicitly required.

### Database rules
- Use `Using` blocks for connections, commands, readers, and tables where applicable.
- Use `SqlCommand.Parameters` for all input values.
- Do not concatenate user input into SQL commands.
- Prefer stored procedures where the database already supports them.
- Centralize stored procedure names and SQL constants where practical.

### Error handling rules
- Do not leave empty `Catch` blocks.
- Log or surface meaningful errors appropriately.
- Replace abrupt termination patterns with controlled shutdown logic.

### Configuration rules
- Wrap registry access in dedicated configuration classes.
- Keep credentials and secrets out of form code.
- Centralize common registry paths and setting keys.

## Recommended Refactoring Roadmap

## Phase 1 - Document and stabilize
Goal: create a safe baseline before deeper changes.

Tasks:
- Keep this README current as the working improvement reference.
- Identify high-risk workflows:
  - login
  - create expense report
  - add expense
  - file report
  - approve/return/cancel report
  - expense summary/export
- Document environment limitations for Windows-only dependencies.
- Avoid large structural moves before behavior is understood.

Expected outcome:
- A clear sequence for future work.
- Shared understanding of what must stay stable.

## Phase 2 - Stop adding new logic to legacy hotspots
Goal: prevent the architecture from getting worse.

Focus files:
- `ER System/mConn.vb`
- `ER System/modLoadingData.vb`
- `ER System/modMaintenance.vb`
- `ER System/modReport.vb`
- `ER System/modReuse.vb`
- `ER System/ModDataStore.vb`
- major forms such as `frmLogin.vb`, `frmEReport.vb`, `frmMain.vb`

Tasks:
- Avoid adding more business logic directly into forms.
- Avoid adding new mutable shared state into modules.
- When a legacy area must be changed, prefer extracting one focused class first.

Expected outcome:
- New code starts moving toward the intended architecture.

## Phase 3 - Extract configuration and connection management
Goal: isolate registry and connection setup from UI code.

First candidates:
- `Infrastructure/Configuration/RegistryConnectionSettingsProvider.vb`
- `Infrastructure/Data/Sql/SqlConnectionFactory.vb`
- `Shared/Utilities/RegistryKeys.vb`

Tasks:
- Wrap reads and writes to registry settings.
- Centralize SQL connection string construction.
- Replace direct use of `mConn.SQLConnection` in newly touched features.
- Centralize encryption usage for connection-related values.

Expected outcome:
- Forms no longer need to know how registry/configuration details are stored.
- Database connection creation becomes reusable and testable.

## Phase 4 - Extract database access by feature
Goal: move data access behind repositories.

Start with one feature at a time, such as:
- user account loading
- login
- expense report loading
- expense details loading

Suggested classes:
- `Infrastructure/Data/Repositories/UserAccountRepository.vb`
- `Infrastructure/Data/Repositories/ExpenseReportRepository.vb`
- `Infrastructure/Data/Sql/StoredProcedureNames.vb`

Tasks:
- Move SQL calls out of forms and global modules.
- Replace concatenated SQL with parameters.
- Keep return values as `DataTable` at first if needed for low-risk migration.
- Later replace `DataTable` returns with typed models where safe.

Expected outcome:
- Database logic becomes isolated and easier to improve.

## Phase 5 - Extract application services
Goal: move workflow decisions out of UI.

Suggested services:
- `Application/Services/UserAccountService.vb`
- `Application/Services/ExpenseReportFilingService.vb`
- `Application/Services/ApprovalWorkflowService.vb`
- `Application/Services/ExpenseSummaryService.vb`

Tasks:
- Move login rules out of `frmLogin.vb`.
- Move report filing and approval decisions out of forms.
- Return simple result objects or models that the form can display.

Expected outcome:
- Forms become smaller and easier to maintain.
- Business rules can be tested without WinForms controls.

## Phase 6 - Clean up forms gradually
Goal: reduce code-behind complexity without breaking UI behavior.

Tasks:
- Break large event handlers into smaller private methods.
- Move validation and save/load behavior to services.
- Keep only UI orchestration in forms.
- Preserve form names and designer relationships.

Examples of good form responsibilities:
- gather user input
- call a service
- bind returned data
- show validation or error messages

Expected outcome:
- Forms remain familiar to current users and developers.
- Code-behind files become easier to understand.

## Phase 7 - Reduce global shared state
Goal: remove hidden dependencies caused by modules.

Tasks:
- Replace module-level `Public` fields with typed models and method parameters.
- Reduce dependence on mutable globals from `modLoadingData` and `ModDataStore`.
- Introduce small request/response models where helpful.

Expected outcome:
- Fewer side effects.
- Better predictability and easier debugging.

## Phase 8 - Introduce domain models and constants
Goal: remove magic strings and weakly typed workflow values.

Candidates:
- report statuses
- approval statuses
- signatory IDs
- registry key names
- common configuration names

Suggested folders:
- `Domain/Enums/`
- `Domain/ValueObjects/`
- `Shared/Utilities/`

Expected outcome:
- Safer code and fewer duplicated strings.

## Phase 9 - Add tests around extracted logic
Goal: improve confidence in refactoring.

Priority:
- test `Application` and `Domain` logic first
- avoid UI tests as the first step

Good candidates for tests:
- validation logic
- login decision rules
- approval routing logic
- expense total calculations

Expected outcome:
- Refactoring can continue with less risk.

## First 3 Recommended Refactoring Targets

### Target 1 - Login workflow
Files involved now:
- `ER System/frmLogin.vb`
- `ER System/mConn.vb`
- related loading/auth functions

Why first:
- High business value
- Clear boundaries
- Mixes UI, DB, registry, and startup behavior

Desired result:
- `frmLogin` becomes a thin UI form
- login validation and user loading move to a service/repository pair

### Target 2 - Connection and registry setup
Files involved now:
- `ER System/mConn.vb`
- connection-related registry reads across the project

Why second:
- Shared dependency across many workflows
- Good foundation for later work

Desired result:
- One place to build SQL connections
- One place to read/write registry-backed settings

### Target 3 - Expense report loading
Files involved now:
- `ER System/frmEReport.vb`
- `ER System/modLoadingData.vb`
- `ER System/ClsLoadData.vb`

Why third:
- Core workflow
- Large methods and multiple responsibilities

Desired result:
- repository handles data access
- service handles workflow
- form handles display and user interaction

## Things to Avoid

- Do not do a full folder move in one change.
- Do not rename many forms at once.
- Do not manually rewrite designer files.
- Do not change database schema unless explicitly required.
- Do not mix structural refactoring with broad feature changes.
- Do not introduce .NET Core or .NET 5+ only APIs into the net48 app.

## Definition of Done for Each Refactoring Step

A refactoring step is considered complete when:
- the application still builds in the intended Windows/Visual Studio environment
- behavior is intentionally unchanged unless otherwise stated
- no designer relationships are broken
- database calls in the touched area are parameterized
- the form in the touched area has less responsibility than before
- any new code is placed in the appropriate layer folder
- key limitations or follow-up work are documented

## Pull Request Checklist

For each future change, record:
- summary of what changed
- layer/folder affected
- whether forms, designer files, SQL, resources, or installer projects changed
- build/test steps attempted
- environment limitations encountered
- whether behavior changed or remained the same

## Working Style Recommendation

When improving this project, prefer this sequence:
1. understand one workflow
2. isolate one seam
3. extract one class
4. update one form to use it
5. verify behavior
6. document the change

This project should be improved in small, reversible, low-risk steps.
