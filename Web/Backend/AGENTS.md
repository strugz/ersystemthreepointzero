# ER System Web Backend Agent Guide

## Purpose and Scope

This guide applies to all files under `Web/Backend/`. It supplements the repository-root `AGENTS.md` and `Web/AGENTS.md`; follow all three guides. If instructions conflict, the repository-root guide is authoritative, followed by `Web/AGENTS.md`, then this backend-specific guide.

The backend is an ASP.NET Core API on .NET 10. It exposes Manager approval and Finance physical-receipt workflows to the Vue frontend while preserving compatibility with the legacy WinForms application and shared SQL Server database. It is intentionally separate from `ER System.sln`, the desktop assemblies, and the desktop installer.

## Runtime and Build Settings

- `ERSystem.Web.sln` is the backend solution.
- `Directory.Build.props` targets `net10.0`, enables nullable reference types and implicit usings, uses the latest configured C# language version, and treats warnings as errors.
- `src/ERSystem.Web.Api/` is the executable host.
- `src/ERSystem.Reminders.Worker/` is a separate `net10.0-windows` executable hosted by Windows Service Manager for unattended approval reminders. It is not an IIS application.
- `tests/ERSystem.Web.Tests/` is the xUnit test project.
- The API and SPA are published together as one same-origin IIS application, but the SPA source and build remain under `Web/Frontend/`.

Keep code compatible with the configured .NET SDK, SQL Server 2008 or later, and database compatibility level 100. Do not use generated SQL that requires a newer compatibility level without a reviewed database upgrade plan.

## Architecture and Dependency Direction

Preserve the existing Clean Architecture boundaries:

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

ERSystem.Reminders.Worker
  -> ERSystem.Web.Application
  -> ERSystem.Web.Infrastructure

ERSystem.Web.Tests
  -> all backend projects under test
```

- Domain contains only pure workflow rules and values.
- Application defines use-case contracts, DTOs, validation concepts, and cross-cutting abstractions.
- Infrastructure implements persistence, authentication compatibility, authorization queries, auditing, transactions, and workflows that touch external systems.
- API owns HTTP transport, cookies, antiforgery, authorization policies, middleware, endpoint mapping, OpenAPI, static SPA hosting, and dependency composition.
- Tests enforce dependency rules and cover behavior across the layers.

Do not reference the legacy VB.NET projects from the web solution. The desktop and web applications integrate through compatible database contracts and equivalent workflow behavior, not direct assembly references.

## Project Responsibilities

### `ERSystem.Web.Domain`

This project must remain framework-independent and have no project dependencies.

| Area | Current function |
| --- | --- |
| `Common/WorkflowRules.cs` | Defines legacy report states, Finance states, audit event names, and the approval-sequence rule requiring all earlier approvers to finish first. |

Put new business invariants here only when they can be evaluated without EF Core, HTTP, configuration, file access, or user-interface types. Add focused unit tests for every non-trivial rule.

### `ERSystem.Web.Application`

This project defines what the backend can do without deciding how SQL Server, cookies, or ASP.NET Core implement it.

| Area | Current function |
| --- | --- |
| `Common/Contracts.cs` | Owns bounded pagination, sort direction, paged results, clock/current-user/row-version/audit/authorization/transaction interfaces, text normalization, and application exception types. |
| `Features/Authentication/AuthenticationContracts.cs` | Defines login input, authenticated-user output, and the authentication service contract. |
| `Features/ManagerApprovals/ManagerApprovalContracts.cs` | Defines Manager list filters, list/detail DTOs, expense, cash advance, receipt, and approval-trail DTOs, approval/return requests, action results, attachment content, and the Manager service contract. |
| `Features/FinanceReceipts/FinanceReceiptContracts.cs` | Defines Finance list filters, list/detail DTOs, receipt-confirmation input/output, and the Finance service contract. |

Application DTOs are the stable boundary between API and Infrastructure. Do not expose Infrastructure entities through these contracts. Keep validation that is independent of persistence close to the relevant request or application service contract.

### `ERSystem.Web.Infrastructure`

This project owns all interaction with the shared database and other external concerns.

#### Authentication and security

| File or area | Current function |
| --- | --- |
| `Authentication/LegacyAuthenticationService.cs` | Normalizes usernames, loads legacy accounts, checks lockout state, compares legacy-encrypted passwords in fixed time, records failed/successful login state, and derives Manager/Finance roles from legacy records. |
| `Security/LegacyPasswordCipher.cs` | Reproduces the desktop application's credential encryption format using the configured legacy encryption key. This is a compatibility seam, not a new password-storage design. |
| `Services/CommonServices.cs` | Provides the UTC clock, reads the authenticated user from claims, and encodes/validates Base64 row versions. |
| `Services/ReportAuthorizationService.cs` | Confirms that the current Manager is assigned to the requested employee report before protected report or attachment access. |

Authentication currently applies a five-failure lockout for 15 minutes within a 15-minute failure window. Do not weaken lockout, fixed-time comparison, role derivation, cookie security, or secret handling without an explicit security requirement and tests.

#### Configuration and startup validation

| File or area | Current function |
| --- | --- |
| `Configuration/ServiceCollectionExtensions.cs` | Validates required configuration, configures both EF Core context factories at compatibility level 100, and registers infrastructure/application-service implementations. |
| `Persistence/DatabaseCompatibilityValidator.cs` | Fails API startup when SQL Server is too old, the database compatibility level is not 100, or required web security/audit tables are missing; logs legacy-server compatibility mode. |

Required secrets and connection settings are:

```text
ConnectionStrings__ErDatabase=<SQL Server connection string>
LegacyAuthentication__EncryptionKey=<existing ER credential encryption key>
```

Use .NET user secrets for local development and protected environment or IIS configuration in deployed environments. Never commit real values.

#### Persistence

| File or area | Current function |
| --- | --- |
| `Persistence/Entities.cs` | Contains EF Core persistence models for mapped legacy tables plus web login-security and workflow-audit tables. These are database entities, not API DTOs. |
| `Persistence/ErDbContexts.cs` | Maps exact legacy table/column names, ANSI/Unicode shapes, keys, row versions, and web-owned tables. |
| `Persistence/QueryableExtensions.cs` | Provides validated sort selection and deterministic in-memory pagination used where SQL compatibility constraints require materializing results first. |

`LegacyErDbContext` maps users, departments, approval assignments, reports, expenses, cash advances, approval signatures, Finance tracking, scanned receipts, and workflow audits. It does not own migrations for legacy tables.

`WebWorkflowDbContext` owns access to web login-security and workflow-audit tables only. Schema changes are deployed through reviewed, dated SQL scripts in the repository-root `Database/` folder; the API must never apply migrations automatically.

#### Workflow services

| File or area | Current function |
| --- | --- |
| `Services/ManagerApprovalService.cs` | Lists reports at a Manager's current step, builds report details, authorizes and streams receipt attachments, enforces ordered approval, records intermediate/final approval state, initializes Finance tracking after final approval, calls legacy notification procedures, returns reports through the legacy procedure, and writes audit events. |
| `Services/FinanceReceiptService.cs` | Lists fully approved reports, loads receipt-tracking detail, validates one-time physical-receipt confirmation, records receiver/date/remarks/status, enforces row-version concurrency, and writes an audit event. |
| `Services/WorkflowAuditWriter.cs` | Inserts parameterized audit rows into `dbo.tbWebWorkflowAudit` using the workflow transaction and correlation ID. |
| `Services/EfTransactionRunner.cs` | Provides serializable transaction execution for use cases requiring an all-or-nothing boundary. |

Approval reminder orchestration lives under `Application/Features/ApprovalReminders`, pure calendar scheduling under `Domain/ApprovalReminders`, and SQL/SMTP/SMS API implementations under `Infrastructure/Reminders`. The worker project only schedules runs, creates a scope, and records sanitized summaries. Preserve the unique delivery claim before external sends and revalidate the current approval step inside the claim transaction.

Manager approval and Finance receipt mutations are security- and data-integrity-sensitive. Preserve report-level authorization, serializable transactions, row-version checks, duplicate/out-of-order conflict checks, legacy stored-procedure behavior, audit writes, and the final-approval handoff to Finance.

### `ERSystem.Web.Api`

The API project translates HTTP requests into application calls and hosts the built SPA.

#### Startup pipeline

`Program.cs` currently configures:

- Controllers, OpenAPI, Swagger UI in Development, and `/health`.
- Strict, Secure, HttpOnly antiforgery and authentication cookies.
- Eight-hour sliding cookie sessions.
- `Manager` and `Finance` role policies.
- Per-IP login rate limiting of 10 attempts per minute.
- Correlation IDs, exception-to-ProblemDetails mapping, HTTPS redirection, authentication, antiforgery, and authorization in the required middleware order.
- Default/static files and an `index.html` fallback for SPA routes.

Middleware order is behavior. Review authentication, antiforgery, authorization, exception handling, and static-file consequences before reordering it.

#### Controllers and routes

| Controller | Route | Authorization | Function |
| --- | --- | --- | --- |
| `AuthController` | `GET /api/auth/antiforgery` | Anonymous | Creates/stores an antiforgery token and returns the request token to the SPA. |
| `AuthController` | `POST /api/auth/login` | Anonymous, rate limited | Validates legacy credentials, creates identity/role claims, and signs in with the cookie scheme. |
| `AuthController` | `POST /api/auth/logout` | Authenticated | Ends the current cookie session. |
| `AuthController` | `GET /api/auth/me` | Authenticated | Returns the current identity and roles from server claims. |
| `ManagerReportsController` | `GET /api/manager/reports` | Manager | Returns the Manager's filtered, sorted, paged report queue. |
| `ManagerReportsController` | `GET /api/manager/reports/{reportId}` | Manager plus report assignment | Returns report, expenses, cash advance, attachments, approval trail, state, and row version. |
| `ManagerReportsController` | `GET /api/manager/attachments/{attachmentId}` | Manager plus report assignment | Streams an authorized scanned receipt with range support. |
| `ManagerReportsController` | `POST /api/manager/reports/{reportId}/approve` | Manager plus report assignment | Approves the Manager's current step using the supplied row version. |
| `ManagerReportsController` | `POST /api/manager/reports/{reportId}/return` | Manager plus report assignment | Returns the report with a required reason and row-version check. |
| `FinanceReportsController` | `GET /api/finance/reports` | Finance | Returns filtered, sorted, paged, fully approved reports for receipt tracking. |
| `FinanceReportsController` | `GET /api/finance/reports/{reportId}` | Finance | Returns Finance and physical-receipt detail for a fully approved report. |
| `FinanceReportsController` | `POST /api/finance/reports/{reportId}/receive` | Finance | Permanently records physical receipt acceptance using row-version concurrency. |

Keep controllers thin: bind and validate the request, obtain the server-side current user and correlation ID, call one application service, and map the result to an HTTP response. Never place workflow SQL or authorization decisions in a controller.

#### Middleware

| File | Current function |
| --- | --- |
| `Configuration/CorrelationIdMiddleware.cs` | Accepts a valid request correlation GUID or creates one, returns it in `X-Correlation-ID`, stores it for audit use, and adds it to the logging scope. |
| `Middleware/AntiforgeryMiddleware.cs` | Validates antiforgery tokens for every unsafe HTTP method under `/api`. |
| `Middleware/ApiExceptionMiddleware.cs` | Maps application, antiforgery, and unexpected exceptions to RFC ProblemDetails with stable statuses, optional machine codes, and correlation information. |

Preserve the established error meanings: validation `400`, forbidden `403`, not found `404`, concurrency/duplicate/order conflicts `409`, and unexpected failures `500`. Do not reveal secrets or sensitive report content in errors.

### `ERSystem.Web.Tests`

| Area | Current function |
| --- | --- |
| `Architecture/DependencyTests.cs` | Prevents Domain/Application dependency reversal and confirms EF contexts stay in Infrastructure. |
| `Unit/WorkflowRulesTests.cs` | Covers approval ordering, pagination limits, legacy cipher compatibility, and row-version behavior. |
| `Unit/InMemoryPagingTests.cs` | Covers page slicing, totals, empty pages, and deterministic ordering. |
| `Unit/ApiExceptionMiddlewareTests.cs` | Covers stable antiforgery error codes and ordinary validation ProblemDetails behavior. |

Add unit tests for pure rules, normalization, validation, mapping, and service decisions. Add integration tests for endpoint authorization, SQL mappings, stored procedures, transactions, row-version conflicts, and audit persistence when the environment supports SQL Server.

## Where New Backend Code Belongs

| New responsibility | Location |
| --- | --- |
| Pure invariant, state value, or workflow rule | `src/ERSystem.Web.Domain/<feature>/` |
| Request/response DTO, use-case interface, or persistence-independent validation | `src/ERSystem.Web.Application/Features/<feature>/` |
| Shared application abstraction or error concept | `src/ERSystem.Web.Application/Common/`, only when multiple features need it |
| EF mapping, database entity, SQL query, repository/service implementation, audit, credential compatibility, file storage, or external integration | Focused folder under `src/ERSystem.Web.Infrastructure/` |
| HTTP route, controller, policy, middleware, ProblemDetails mapping, health check, or host composition | Focused folder under `src/ERSystem.Web.Api/` |
| Unit, architecture, or SQL integration test | Matching area under `tests/ERSystem.Web.Tests/` |
| Schema or stored-procedure change | New dated forward and appropriate rollback scripts under repository-root `Database/` |

Do not add a generic repository over EF Core, MediatR, base controllers, global service locator, or catch-all utility module. Extract a shared abstraction after two genuine call sites or when it centralizes a safety rule such as authorization, transactions, auditing, concurrency, or error mapping.

## API and Data Rules

- Prefix application endpoints with `/api` and use async operations with `CancellationToken`.
- Use typed request and response DTOs; never expose EF entities.
- Treat server claims as the identity source. Never authorize from a user ID supplied by the client.
- Default pagination to 25 and cap page size at 100. Validate sort fields against an allowlist and include a deterministic tie-breaker.
- Use ISO-8601 dates, UTC timestamps, and Base64 row versions in API contracts.
- Use `AsNoTracking()` for reads and project only required columns where SQL compatibility permits.
- Parameterize raw SQL and stored-procedure inputs. Never concatenate report IDs, user input, or file paths into SQL.
- Use transactions for multi-table workflow changes and include the audit write in the same transaction.
- Return `409 Conflict` for stale row versions, duplicate actions, incomplete prior approval steps, and invalid workflow state transitions.
- Restrict attachment access by both role and report assignment. Do not trust metadata or paths supplied by the client.
- Coordinate changes to legacy tables, statuses, procedures, or encryption with the desktop application and mixed-version deployment plan.

## Security and Logging Rules

- Require API authorization policies even when the frontend hides a route or button.
- Validate antiforgery tokens for every cookie-authenticated mutation, including anonymous login.
- Keep authentication and antiforgery cookies Secure, HttpOnly, and same-origin compatible.
- Keep login rate limiting and lockout controls enabled.
- Never log passwords, encrypted passwords, cookies, antiforgery tokens, encryption keys, connection strings, receipt contents, signatures, or full sensitive request bodies.
- Use structured logs with operation, report ID, actor ID, and correlation ID only when appropriate and non-sensitive.
- Log expected application failures without stack-trace noise; log unexpected exceptions with enough server-side context to investigate.
- Do not silently catch failures or convert database conflicts into success.

## Verification

From `Web/Backend` run the checks appropriate to the change:

```text
dotnet restore ERSystem.Web.sln
dotnet build ERSystem.Web.sln
dotnet test ERSystem.Web.sln
dotnet format ERSystem.Web.sln --verify-no-changes
```

For database-facing changes, also verify against a supported SQL Server instance at compatibility level 100. Exercise missing records, unauthorized report access, invalid filters, maximum page size, duplicate actions, stale row versions, transaction rollback, audit persistence, and required legacy stored-procedure effects.

If SQL Server, the required .NET SDK, IIS, or legacy configuration is unavailable, state exactly which verification could not run. Do not claim database or deployment success from a compile-only check.

## Prohibited Changes

- Do not reference API, Infrastructure, ASP.NET Core, EF Core, configuration, WinForms, or frontend types from Domain.
- Do not reference API or Infrastructure from Application.
- Do not put business SQL or workflow decisions in controllers or middleware.
- Do not expose database entities, client-selectable identities, secrets, or sensitive receipt contents through errors or logs.
- Do not bypass report-level authorization, approval ordering, antiforgery, row-version checks, transactions, or audit writes.
- Do not let EF Core create or migrate the legacy database automatically.
- Do not execute or rewrite `ER3.0.sql` as a migration.
- Do not change legacy statuses, table mappings, encryption, or stored-procedure behavior without tracing desktop compatibility.
- Do not add generated `bin/`, `obj/`, publish output, user-specific project files, or secrets to source control.
