# ERFWeb

Web-based Expense Report (ERF) system. Standalone project — **not** part of the desktop
solution `ER System.sln` or the installer, and separate from the existing `Web/` portal.

- **Backend:** ASP.NET Core (net10.0), EF Core on **PostgreSQL** (Npgsql). Data access is
  **EF Core + LINQ only** (no stored procedures). API docs via Swagger.
- **Frontend:** Vue 3 + Vuetify + Vite + TypeScript SPA.

See `../ER System 3.0/CLAUDE.md` context and the project documentation for the full plan.

## Layout

```
ERFWeb/
  Backend/   ERFWeb.sln  ->  src/{Api,Application,Domain,Infrastructure}  +  tests/ERFWeb.Tests
  Frontend/  erfweb-client (Vue 3 + Vuetify SPA)
```

Clean Architecture dependency direction: `Api -> Application -> Domain`,
`Infrastructure -> Application -> Domain` (enforced by `tests/.../Architecture/DependencyTests.cs`).

## Prerequisites

- .NET SDK 10
- Node.js 20.19+ (built with 24.x)
- PostgreSQL 13+ (for `gen_random_uuid()` and `jsonb`)

## Backend

From `ERFWeb/Backend`:

```bash
dotnet restore ERFWeb.sln
dotnet build ERFWeb.sln -c Debug
dotnet test ERFWeb.sln
dotnet run --project src/ERFWeb.Api      # http://localhost:5080, Swagger at /swagger
```

### Configuration — `appsettings.json` is the single source of truth

**`appsettings.json` is gitignored and never committed** (it holds the database password). On a fresh
clone, create it from the committed template, then fill in the real values:

```bash
copy src\ERFWeb.Api\appsettings.Example.json src\ERFWeb.Api\appsettings.json
```

Because it is untracked, switching branches (`git switch` / `git checkout`) leaves it untouched.
Never run `git clean -x` (the `-x` flag deletes ignored files) and never `git add -f` it.

The API reads **only** `src/ERFWeb.Api/appsettings.json` plus the environment overlay
`appsettings.{Environment}.json` (e.g. `appsettings.Development.json`). Environment variables,
user secrets, and hard-coded fallbacks are **not** used — there is nothing configured anywhere
else. A missing required key stops startup with a clear error naming the key.

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:Postgres` | PostgreSQL connection string (**the one place it lives**) |
| `Authentication:SessionCookieName` | Session cookie name (`ERFWeb.Session`) |
| `Authentication:SessionHours` / `SlidingExpiration` | Session lifetime and sliding renewal |
| `Authentication:AntiforgeryCookieName` / `AntiforgeryHeaderName` | Antiforgery cookie + header (`X-CSRF-TOKEN`; the SPA client sends this same header) |
| `Cors:AllowedOrigins` | SPA origins allowed to call the API with cookies |
| `Swagger:Enabled` | Swagger UI on/off (`false` in base, `true` in `appsettings.Development.json`) |
| `R47Report:*` | R47 form constants: company, document code, effectivity date, revision, rows per page, UTC offset, fonts, QuestPDF license tier |
| `Attachments:MaxFileSizeMb` / `MaxFilesPerReport` / `AllowedContentTypes` | Receipt upload limits (also sets the Kestrel/multipart body limit) |
| `ReferenceNumber:Prefix` / `Digits` / `UtcOffsetHours` | ERF reference number series, e.g. `ERF-2026-000142` (prefix, zero-padding, which local year a filing belongs to) |
| `Logging:*`, `AllowedHosts` | Standard ASP.NET Core |

Which environment overlay applies is chosen by `ASPNETCORE_ENVIRONMENT` (set in
`Properties/launchSettings.json` for local runs) — that selects the file; it never supplies values itself.

### Database setup — Option A: run the SQL scripts (current approach)

The canonical schema lives in `Database/` and matches the EF Core model exactly. Run in order
against an empty database (PostgreSQL 13+):

```bash
createdb erfweb
psql -d erfweb -f Database/001_initial_schema.sql    # tables, indexes, history trigger
psql -d erfweb -f Database/002_seed_admin.sql        # dept + admin login (admin / ChangeMe!123 — change it)
psql -d erfweb -f Database/003_reference_series.sql  # yearly ERF reference number counters
psql -d erfweb -f Database/004_seed_test_users.sql  # optional: employee1 / manager1 / manager2 / finance1 (password Test!1234) for a filing walkthrough
psql -d erfweb -f Database/005_cash_advance_lines.sql # cash advances become rows (replaces the one-row cash_advances table)
psql -d erfweb -f Database/006_user_roles_and_administration.sql # Phase 1: additive roles (user_roles), master data, rules, second admin
```

Both scripts are idempotent (safe to re-run). Do **not** also run `dotnet ef database update`
against the same database unless you first baseline the migration history.

### Database setup — Option B: EF Core migrations (when EF tooling is available)

The DbContext and entities are migration-ready. Generate and apply the initial schema:

```bash
dotnet tool install --global dotnet-ef        # if not already installed
dotnet ef migrations add InitialCreate \
  --project src/ERFWeb.Infrastructure --startup-project src/ERFWeb.Api \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project src/ERFWeb.Infrastructure --startup-project src/ERFWeb.Api
```

After generating `InitialCreate`, append the row-history trigger objects to its `Up()` (and the
drops to `Down()`) using the SQL in `src/ERFWeb.Infrastructure/Persistence/HistorySql.cs`:

```csharp
migrationBuilder.Sql(ERFWeb.Infrastructure.Persistence.HistorySql.CreateFunction);
migrationBuilder.Sql(ERFWeb.Infrastructure.Persistence.HistorySql.CreateTriggers);
// Down():
migrationBuilder.Sql(ERFWeb.Infrastructure.Persistence.HistorySql.DropAll);
```

### Schema notes

- **Enums** are stored as readable text (e.g. `'ForApproval'`) via EF string conversions.
- **Concurrency** uses the PostgreSQL system column `xmin` as the token.
- **Row history** on the high-churn tables (`expense_reports`, `expense_lines`, `approval_steps`,
  `finance_tracking`, `cash_advance_lines`) is captured by the `fn_record_history` trigger into
  `record_history`; the app sets `SET LOCAL app.current_user_id` per transaction to record the actor.
- snake_case table/column naming via `EFCore.NamingConventions`.

## Frontend

From `ERFWeb/Frontend/erfweb-client`:

```bash
npm ci
npm run dev          # http://localhost:5173, proxies /api and /health to VITE_API_TARGET (see below)
npm run build        # type-check (vue-tsc) + production build
npm run lint
npm run type-check
```

The dev proxy target is `VITE_API_TARGET` in `.env` (default `http://localhost:5080`, the Kestrel
`http` launch profile). If you run the API from Visual Studio with the **IIS Express** profile, set it to
`https://localhost:44387` instead; the proxy accepts the self-signed dev certificate (`secure: false`).
Put a machine-specific value in `.env.local` (gitignored) rather than editing `.env`.

## Reporting — R47 "EXPENSE REPORT" (plan Phase 3)

`GET /api/reports/{reportId}/pdf` streams the R47 controlled form as PDF (inline; add `?download=true`
for an attachment). Access: Finance/Admin see all; an employee sees their own; an approver sees reports
with an approval step assigned to them.

Pipeline: `ExpenseReportDocumentLoader` (LINQ read model) → Domain `R47Bucketing` / `R47Layout`
(column bucketing — each column carries the line total (amount × multiplier), VAT is its own memo column —
per-date "Work With" grouping, Crystal-style pagination by height — `R47Report:RowsPerPage` is the detail area in text lines, wrapped particulars count for more, a group header is never orphaned and is repeated with its date when a group continues on the next page — and the running cash-advance reconciliation: each page carries `cash advance − prior pages' expenses`, less this page's total, giving that page's BAL DUE) → `R47Document` (QuestPDF, US Letter landscape) → `R47PdfRenderer`.

Form constants (company, document code, effectivity date, revision, rows per page, UTC offset, QuestPDF
license tier) live in the `R47Report` section of `appsettings.json` (`R47ReportOptions`). The MDMPI logo
is an embedded resource. Fidelity is checked against the reference printout: `R47PdfRendererTests`
writes `R47-sample-page1.pdf` to the test output folder for side-by-side comparison.

> **License:** ERFWeb uses QuestPDF under the **Community** license (free, including commercial use)
> because MDMPI's annual gross revenue is under the USD 1M threshold. The threshold is assessed on the
> whole organisation including affiliates/parent group — re-verify at questpdf.com/license if that
> changes, and set `R47Report:QuestPdfLicense` to `Professional`/`Enterprise` if a paid tier is ever needed.

SPA: `/reports/:id/pdf` (view / print / download / open in new tab); the dashboard has a report-ID launcher.

## Authoring & filing (plan Phase 4)

Employees create, edit, and file their own ERFs; everything is EF Core + LINQ inside the serializable
`ITransactionRunner` (commit on success, rollback on any failure). Each transaction first publishes the
acting user to PostgreSQL (`set_config('app.current_user_id', …, true)`) so the `fn_record_history`
trigger stamps `record_history.changed_by`.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/reports` | My reports, paged (`page`, `pageSize`, `sort`, `direction`, `status`, `search`) |
| `POST /api/reports` · `GET/PUT/DELETE /api/reports/{id}` | Create / read / update header + cash advance / delete (never-filed only) |
| `POST /api/reports/{id}/lines` · `PUT/DELETE …/lines/{lineId}` | Expense lines with meal / fare-leg items |
| `POST /api/reports/{id}/cash-advances` · `PUT/DELETE …/cash-advances/{lineId}` | Rows of the CASH ADVANCE RECEIVED block (Liquidation of Cash Advance only); the R47 reconciles against their total |
| `GET/POST /api/reports/{id}/attachments` · `GET …/{aid}/content` · `DELETE …/{aid}` | Receipt scans (`multipart/form-data`, field `file`); stored as `bytea` in `receipt_attachments`, not on disk. The browser shrinks JPEG/PNG photos first (`src/shared/images.ts`: longest side 1600 px, JPEG 80 %, EXIF orientation applied, PDFs untouched, original kept if not smaller) |
| `POST /api/reports/{id}/file` | File / re-file → `ForApproval`; snapshots the approval route into `approval_steps` |
| `POST /api/reports/{id}/reopen` | Pull a `ForApproval`/`Approved` report back to `New` (supersedes the cycle) |
| `GET /api/lookups/work-with` · `my-rates` · `my-filing-readiness` | Editor lookups |

Rules (Domain, unit-tested): a report is editable only while `New` or `Returned`; filing requires ≥ 1 active
line, the employee's signature on file, a configured approval route, a purpose, a cash-advance amount for a
Liquidation of Cash Advance, and a revolving-fund amount for a Replenishment of Revolving Fund. The money block
follows the type exactly as the desktop form did: only a Liquidation stores the CASH ADVANCE RECEIVED block, only a
Replenishment stores the revolving fund, a Reimbursement stores neither. A line's total is **amount × multiplier**
(per-diem: `(days − minus) × daily`) rounded the desktop way: a receipt-backed line with multiplier 1 keeps centavos,
every other line rounds to whole pesos (midpoint to even, like VB `Math.Round`). VAT is a separately entered memo
figure allowed only with an Invoice/OR number. **BAL DUE TO** on the R47 is never stored: it follows the sign of
`cash advance − expenses` (`BalanceDueRules`) — positive is due to MDMPI, otherwise due to the employee — because the
legacy `sp_rptER` never read the old `BalanceTo` column and the desktop always wrote `'EMPLOYEE'`. Re-filing after a return
opens a new `cycle`; reopening supersedes pending steps and withdraws Finance tracking. Every mutation
carries the row's `rowVersion` (`xmin`); a stale value → **409** and the SPA reloads.

**ERF reference number.** Issued by the system on **first filing** as a yearly series
`ERF-2026-000142` (`ReferenceNumberFormat` + `ReferenceNumberGenerator`): one counter row per
`PREFIX-YYYY` in `reference_series`, incremented inside the filing transaction. Two filings racing for the
same number make PostgreSQL fail one with a serialization error, which the DbContext's
`EnableRetryOnFailure` replays through the transaction runner, so the series stays unique and gapless
(deleted drafts never consume a number; returns and re-files keep theirs). Drafts show "Assigned when filed".

Header/line updates, deletes, filing and reopening write `workflow_audit` rows in the same transaction.
Filing hands a `ReportFiledNotification` to `IApprovalNotifier`; Phase 2 ships the logging implementation
(`LoggingApprovalNotifier`) — SMTP/SMS delivery plugs into the same seam in Phase 4 and can never roll
back a filing (it runs after commit).

SPA: **My reports** (`/reports`, status/search filters, deep-linkable `?status=`), **New report**
(`/reports/new`), and the **editor** (`/reports/:id`: header + cash advance, grouped expense-lines table,
add/edit line dialog with meal chips, fare legs and per-diem preview, receipts panel, approval timeline,
filing checklist, file / re-file / reopen / delete with confirmations).

## Auth (plan Phase 0)

Cookie session (`ERFWeb.Session`) + antiforgery (`X-CSRF-TOKEN`). Endpoints:
`GET /api/auth/antiforgery`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`,
`GET /api/health`. Passwords verify with PBKDF2; the administration console (below) creates users, or seed one
with a `Pbkdf2PasswordHasher.Hash` value. The session's Role claims are the account's **Active** grants in
`user_roles` plus the implicit `Employee`. Lockout/rate-limiting arrives in a later phase.

## Administration & accounts (plan Phase 1, RBAC v0.6)

Roles are additive rows in `user_roles` (script `006`): `Employee` (implicit, everyone), `SignatoryLevel1`,
`SignatoryLevel2`, `Finance`, `ExpenseVerifier`, `SystemAdministrator`, and `BusinessOwner` (the designated
countersigner, CFO or HR Head, modelled as a grant so it gets the same dual-approval trail). The old
`users.role` column is migrated by `006` (`Manager` → `SignatoryLevel1`, `Admin` → `SystemAdministrator`) and dropped.

Rules enforced by the API (Domain `RoleRules` / `AccountRules`, unit-tested):

- **Dual approval (9.8).** A privileged grant saves as `PendingActivation`; it becomes `Active` only when the
  Business Owner countersigns. While no Business Owner exists, a *second* System Administrator may countersign
  and the grant is flagged `isBootstrap` (plan open item 12.3a). The granter can never countersign their own grant.
- **No self-change (9.7).** An administrator cannot edit, deactivate, reset, or change roles on their own account,
  which is why `006` seeds a second administrator (`admin2`).
- **Stacking (section 3).** A System Administrator holds no transacting role; Finance cannot be a Signatory;
  Finance cannot be an Expense Verifier. Violations return 400 with `errors.role`.
- **Separation of duties.** A System Administrator never sees report contents (`ReportAccessPolicy`) and never
  edits an ERF (`ReportEditGate` is owner-only; there is no proxy filing).
- **Cooling period (9.11).** A new account can file only after `Business.CoolingPeriodDays` (default 3) have
  passed since registration; `my-filing-readiness` reports `coolingPeriodElapsed` / `coolingPeriodEndsAt`.
- Every change writes an `Admin.*` row in `workflow_audit` inside the same transaction; `GET /api/admin/audit`
  is read-only for System Administrators **and Finance** (9.10).

| Endpoint (policy) | Purpose |
| --- | --- |
| `GET/POST /api/admin/users`, `GET/PUT …/{id}`, `POST …/{id}/active`, `…/password`, `…/signature` (SystemAdministrator) | Register and maintain accounts; load the signature printed on the R47 |
| `POST /api/admin/users/{id}/roles` · `POST …/roles/{role}/countersign` (BusinessOwner or SystemAdministrator) · `POST …/roles/{role}/revoke` | Grant, countersign, revoke |
| `GET/POST/PUT /api/admin/master-data/{departments,cost-centers,currencies,sites}` · `GET …/expense-categories` | Master data (deactivate, never delete) |
| `GET/PUT /api/admin/settings` | Workflow rules (Level 2 threshold, SLAs, escalation), business rules (filing cut-off, receipt threshold, cooling period), notification switches; stored in `app_settings` |
| `GET /api/admin/audit?event&actorUserId&from&to` (SystemAdministrator, Finance) | Append-only audit trail |

First-time setup (plan flow 1): sign in as `admin`, create accounts and master data, set the rules; grant the extra
roles and have `admin2` countersign them until a Business Owner is active, after which only the Business Owner
countersigns. Scope assignments and rate profiles (Level 2 only) are plan Phase 2; until then `approval_routes`,
`employee_rates`, and `department_signatories` are still maintained by SQL (see `004_seed_test_users.sql`).
```
