# Codex Instructions for ER System 3.0

## Project Context

- This repository contains a legacy VB.NET Windows Forms application.
- The main solution is `ER System.sln`.
- The application targets .NET Framework 4.8.
- The current codebase is form-heavy and includes legacy global modules with shared state.
- The long-term goal is safer, incremental improvement without breaking the existing WinForms application.

## Working Style

- Make small, buildable changes.
- Preserve existing behavior unless the task explicitly asks for a behavior change.
- Avoid big-bang rewrites, broad file moves, or large structural changes.
- When refactoring, move one focused responsibility at a time and verify after each meaningful change.
- Prefer existing project patterns unless they conflict with the improvement direction in `README.md`.
- Keep changes scoped to the requested task.
- Do not clean up unrelated files or revert user changes.

## Architecture Direction

- Keep forms focused on UI concerns:
  - reading and validating control input
  - calling services, presenters, or repositories
  - displaying results, messages, and visual state
- Put new business logic into focused application services or use-case classes.
- Put new data access behind repository or infrastructure classes.
- Put registry, connection string, and configuration access behind dedicated configuration classes.
- Treat legacy modules as compatibility boundaries, not as places for new design.
- Avoid adding new mutable `Public` global state to modules.
- Prefer typed return values and explicit dependencies over shared state.

## VB.NET / WinForms Rules

- Keep .NET Framework 4.8 compatibility.
- Use `Option Strict On` in new VB files when practical.
- Use clear, descriptive names for classes, methods, variables, and controls.
- Prefer classes over modules for new behavior.
- Do not manually edit `.Designer.vb` files unless the task explicitly requires it.
- Preserve WinForms designer relationships when moving or renaming files.
- Keep event handlers thin; extract workflow, validation, data access, and configuration logic into focused collaborators.
- Do not add new SQL, registry, email, reporting, or business workflow logic directly into forms.

## Comments

- Add concise comments when they explain complex business rules, legacy compatibility decisions, database or configuration behavior, or non-obvious WinForms workflow logic.
- Prefer comments that explain why code exists over comments that describe each line.
- Avoid obvious comments that simply repeat what the code already says.
- Preserve existing comments unless they are clearly wrong or misleading.

## Database / Configuration Rules

- Use `Using` blocks for connections, commands, readers, adapters, and other disposable database objects.
- Use `SqlCommand.Parameters` for input values.
- Do not concatenate user input into SQL commands.
- Prefer stored procedures where the existing database already supports them.
- Centralize stored procedure names, SQL constants, registry paths, and setting keys where practical.
- Keep credentials and secrets out of form code.
- Wrap registry access in dedicated configuration classes.

## Error Handling

- Do not add empty `Catch` blocks.
- Preserve useful exception context.
- Log, return, or surface meaningful errors as appropriate for the existing workflow.
- Avoid abrupt termination patterns such as `End`; prefer controlled shutdown or clear error handling.

## Testing And Verification

- Build the solution after meaningful code changes when the local environment supports it.
- Run focused tests when changing test-covered infrastructure, data access, or business logic.
- For documentation-only changes, runtime tests are not required.
- If a build or test cannot be run because of local environment limits, state that clearly.
- Keep verification proportional to the risk and blast radius of the change.

## Do Not Do

- Do not rewrite the application from scratch.
- Do not move many forms, modules, or project files at once.
- Do not change generated designer code casually.
- Do not introduce new global mutable state.
- Do not place new business rules or data access in WinForms code-behind.
- Do not replace established behavior without an explicit requirement.
- Do not make unrelated formatting, cleanup, or project-file churn.
- Do not hide errors with silent catches.
