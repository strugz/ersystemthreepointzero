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
psql -d erfweb -f Database/007_scope_and_rate_profiles.sql # Phase 2: scope assignments and rate profiles (migrates routes and rates)
psql -d erfweb -f Database/008_approvals_and_finance.sql # Phase 5: line adjustments and Finance receipt tracking columns
psql -d erfweb -f Database/009_verification_requests.sql # Phase 6: verification requests and queries
psql -d erfweb -f Database/010_seed_test_verifier.sql   # optional, dev/UAT only: verifier1 / Test!1234 (needs 004)
psql -d erfweb -f Database/011_sms_deliveries.sql        # Lasius SMS delivery log
psql -d erfweb -f Database/012_sms_deliveries_report.sql # SMS deliveries linked to a report
psql -d erfweb -f Database/013_approval_delegations.sql  # Phase 7: approval delegation and "on behalf of" steps
```

Run the scripts in order on a new database. `004` inserts into the `users.role` column that `006` drops, so it
cannot be run on its own after `006`; on an existing database, add test users through the administration console.

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
npm run dev          # http://localhost:5173, proxies /api (including /api/health) to VITE_API_TARGET (see below)
npm run build        # type-check (vue-tsc) + production build
npm run lint
npm run type-check
```

The dev proxy target is `VITE_API_TARGET` in `.env` (default `http://localhost:5080`, the Kestrel
`http` launch profile). If you run the API from Visual Studio with the **IIS Express** profile, set it to
`https://localhost:44387` instead; the proxy accepts the self-signed dev certificate (`secure: false`).
Put a machine-specific value in `.env.local` (gitignored) rather than editing `.env`.

When the target is a reverse proxy that already maps to the API's `/api` root (the `sr.mdmpi.com.ph` nginx rule
forwards `/erf/*` to `/api/*`), set `VITE_API_PROXY_STRIP_PREFIX=true` so the dev proxy drops the SPA's `/api`
prefix. For a production build served behind that rule, set `VITE_API_BASE_URL` to the public prefix (e.g. `/erf`).

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
countersigns. `department_signatories` (the initials printed on the R47 footer) is still maintained by SQL.

## Scope & rate profiles (plan Phase 2, Level 2 only)

Script `007` adds `scope_assignments` (+ `scope_assignment_members`) and `rate_profiles`, `rate_profile_lines`,
`rate_profile_assignments`, and copies the old `approval_routes` and `employee_rates` rows across as bootstrap
records, so existing employees keep their approvers and rates. The old tables are kept but no longer read.

**Scope: who signs for whom.** An assignment gives a signatory (holder) a Level 1 or Level 2 scope over a
department (`Department`), named people (`Individual`), or a department minus named people (`Exclusion`), with an
optional approval limit and effective dates. Domain `ScopeRules` resolves an employee on a date: a by-name
assignment beats a by-department one, and within the winning tier there must be exactly one holder. A save that
would give anyone two signatories at one level is refused (400 `errors.scope`). `GET /api/scope/coverage` lists
every active employee with their resolved Level 1 and Level 2, or the problem.

**Routing at filing** (Domain `ApprovalRouting`, replaces `approval_routes`). Level 1 comes from scope. A Level 2
step is added when the rules say Level 2 is always required (the default), when the total reaches the Level 2
threshold, when the total exceeds the Level 1 approval limit, or when the claimant is a signatory. A Level 1 who
files their own report goes to their Level 2 alone, and nobody is ever routed to their own report (9.1). The steps
are snapshotted into `approval_steps` (with `signatory_level`) so later scope changes never re-route a filed report
(9.3). Zero matches or an ambiguous match block filing with the reason.

**Rate profiles.** A profile holds versioned lines per category (Meals per meal: Breakfast, Lunch, Dinner, OTMeal).
A change never edits a line: it adds a version and closes the open one the day before, so a report always uses the
rate in force on its expense date. A profile reaches a person by assignment, resolved Individual > Grade >
Department > Company on the expense date. A change that starts before today needs `backdateOverride` and a reason.
`GET /api/lookups/my-rates?date=` gives the line editor the rates in force on that date. A person assigned a
profile gets only that profile's lines; categories it does not define are zero.

**Authority** (Domain `EntitlementAuthority`). Level 1 scope: a Level 2 Signatory or the Business Owner. Level 2
scope: the Business Owner; before one exists, another Level 2 may set it, and a System Administrator may create it
only while no Level 2 covers the area; both are flagged `isBootstrap`. Nobody changes a scope they hold (9.13).
Rates: a Level 2 Signatory or the Business Owner (9.15), never for a target that includes themselves, by person,
grade, or department (9.12); the company default is the Business Owner's (a Level 2 bootstrap before one exists).
Finance and Level 1 read scope and rates; every change is in the audit trail Finance reviews.

| Endpoint (policy) | Purpose |
| --- | --- |
| `GET /api/scope/assignments` · `GET …/{id}` · `GET /api/scope/coverage?date` (signatories, Business Owner, Finance, System Administrator) | Read scope and who covers whom |
| `POST /api/scope/assignments` · `PUT …/{id}` (Level 2, Business Owner, System Administrator; service decides) | Create; change members, limit, end date, active |
| `GET /api/rates/profiles` · `GET …/{id}` · `GET /api/rates/assignments` · `GET /api/rates/resolve?userId&date` | Read profiles, versions, assignments, what applies to a person on a date |
| `POST/PUT /api/rates/profiles` · `POST …/{id}/lines` · `POST /api/rates/assignments` · `POST …/{id}/end` (Level 2, Business Owner) | Maintain profiles, add rate versions, assign and end |
| `GET /api/lookups/departments` (any signed-in user) | Department picker |

Change notifications to affected employees (9.14) go through `IEntitlementChangeNotifier`; like the filing
notifier it only logs until SMTP delivery lands.

## Approvals & Finance receipts (plan Phase 5)

Script `008` adds the line-adjustment columns to `expense_lines` (`original_amount`, `original_total_amount`,
`adjusted_by_user_id`, `adjusted_at`, `adjustment_reason`) and the clearance and follow-up columns to
`finance_tracking`. New status text needs no schema change: reports can be `Rejected`; steps can be `Rejected`
or `Overridden`.

**Approver actions** (Domain `ApprovalWorkflow`, one serializable transaction with the audit row):

- **Approve**: the current step only; steps go in order, so acting out of turn or twice is 409. The last
  approval approves the report and opens its Finance tracking row.
- **Return** (reason required): the report goes back to the employee; later steps are superseded and approval
  restarts from the first approver when it is re-filed.
- **Reject** (reason required): final. The report cannot be re-filed or reopened.
- **Override** (Level 2 only, reason required): approves past a pending Level 1, which is marked `Overridden`.
- **Lower a line** (`PUT /api/approvals/{id}/lines/{lineId}`): only the current Level 1, only before the first
  approval, only downwards, with a reason. The filed amount is kept and shown to everyone. Amounts lock after
  the first approval.

Nobody acts on their own report (9.1). Each action sends the report's `rowVersion` (a line adjustment sends the
line's), so a stale screen gets 409 and reloads. The employee can reopen a filed report only until an approver
acts; after that they ask the approver to return it.

**Finance receipt queue** (Domain `FinanceRules`): `Pending` → `ReceiptsIncomplete` (remarks name what is
missing) → `ReceiptsReceived` (one-time, never reverted) → `ClearedForPayment` (final). Follow-ups are allowed
while receipts are outstanding and are counted. Finance never processes its own report (403), and out-of-order
steps are 409.

| Endpoint (policy) | Purpose |
| --- | --- |
| `GET /api/approvals?view=Pending\|Acted&search&page` (Level 1, Level 2) | Approver inbox: my turn, waiting for an earlier step, or already acted |
| `POST /api/approvals/{id}/approve` · `/return` · `/reject` · `/override` (Level 1, Level 2; service decides) | Body `{ note, rowVersion }` |
| `PUT /api/approvals/{id}/lines/{lineId}` (current Level 1) | Body `{ amount, reason, rowVersion }` |
| `GET /api/finance/queue?status&openOnly&search&page` (Finance) | Approved reports awaiting receipts or clearance |
| `POST /api/finance/{id}/receipts` · `/clear` · `/follow-up` (Finance) | Record receipts `{ status, remarks, rowVersion }`, clear `{ remarks, rowVersion }`, follow up `{ message }` |

`GET /api/reports/{id}` now also returns `review` (what the viewer may do as an approver, from the same rules)
and `finance` (receipt status). SPA screens: **Approvals** (`/approvals`) and the review page (`/review/{id}`)
for signatories, **Finance queue** (`/finance`) for Finance. Workflow notifications go through
`IApprovalNotifier.NotifyWorkflowEventAsync`; like the other notifiers it only logs until SMTP delivery lands.

## Verification & queries (plan Phase 6, Flow 6)

Script `009` adds `verification_requests` (row history through `fn_record_history`, one unresolved request per
line by a partial unique index). The optional `010` seeds a test Expense Verifier, `verifier1`, for walkthroughs.

**Flow** (Domain `VerificationRules`, each step one serializable transaction with its audit row):

1. **Finance raises** a request on one line of an **approved** report that is not yet cleared (9.18), with the
   reason. The report stays in the Finance queue marked *Under verification*; receipts can still be recorded,
   but **clearing for payment is refused (409)** while any request is unresolved.
2. **An Expense Verifier picks it up** from the pool. Nobody verifies their own report, one they approved, or
   one they raised (9.16); those requests are left out of their pool and refused (403).
3. **The verifier records a finding**, once (9.17): *Verified* (closes the request), *Verified with note*
   (closes it; note required), or *Not verified* (note required; goes back to Finance). The verifier never
   changes amounts, approves, or rejects.
4. **Finance queries Level 2** on a Not verified finding. The query goes to the Level 2 who approved the report,
   or, for a route without a Level 2 step, to whoever holds Level 2 scope over the employee today.
5. **Level 2 decides** (9.4, Finance never rejects on merit): *let it through* (Finance carries on), *return*
   (reason required; the report goes back to the employee, leaves the Finance queue, and approval restarts on
   re-filing), or *reject* (reason required; final). Returning or rejecting closes the report's other requests.

**Visibility.** A verifier sees a report only while it has a request in the open pool or one they picked up. A
Level 2 sees reports they were queried about. The claimant sees which line is under verification, its status,
the finding, and Level 2's decision, but not Finance's reason, the verifier's notes, or the query (open decision
9c, defaulted to not showing findings to the claimant until it is decided).

| Endpoint (policy) | Purpose |
| --- | --- |
| `POST /api/verifications` (Finance) | Raise `{ reportId, lineId, reason }` |
| `GET /api/verifications?view=Open\|Mine\|Done` (Expense Verifier) | The pool, my requests in progress, my findings |
| `POST /api/verifications/{id}/pick-up` · `/finding` (Expense Verifier) | `{ rowVersion }` · `{ finding, note, rowVersion }` |
| `POST /api/verifications/{id}/query` (Finance) | Refer a Not verified finding to Level 2 `{ note, rowVersion }` |
| `GET /api/verifications/queries?view=Pending\|Decided` · `POST /api/verifications/{id}/decision` (signatories; service checks it is addressed to you) | Level 2's queries; decide `{ disposition: Resume\|Return\|Reject, note, rowVersion }` |

Every action returns the report detail, which now carries `verification` (the requests with what the viewer may
do) and `finance.underVerification`; Finance queue rows carry `openVerifications` and `needsQuery`. SPA:
**Verifications** (`/verifications`) for Expense Verifiers, a **Queries** tab in Approvals for Level 2, and a
Verification card on the review and report pages. Events are written as `VerificationRequested`,
`VerificationPickedUp`, `VerificationFound`, `QueryRaised`, and `QueryDecided`.

## Notification SMS (Lasius gateway)

Script `011` adds `sms_deliveries`; `012` links each row to its report (`report_id`). `POST /api/sms` (Level 1,
Level 2, Finance, Expense Verifier) takes `{ contactId, message, idempotencyKey?, reportId? }`: `contactId` is the
recipient's `users.id`, and the server texts the Philippine mobile numbers in that user's `mobile_no` (several may
be separated by `,` `;` or `/`). With `reportId`, `contactId` must be the report's claimant, and the report's
Level 1 and Level 2 signatories on the current approval cycle are texted in the same message. Whoever has a
mobile number receives it, and a shared number is texted once. The client never supplies a phone number. The API
posts `{ sender, recipients, message, application }` to Lasius, with `sender` set to the signed-in user's full
name and the API key in a request header. The Finance queue's **Follow up** sends this SMS with the report, then
counts the follow-up.

**Filing SMS:** when an employee files (or re-files after a return) a report, `SmsApprovalNotifier` sends one SMS
to the employee and the report's Level 1 and Level 2 signatories ("ERF-2026-000001 was filed by … and is waiting
for approval in ERFWeb."). It runs after the filing commits and never fails a filing; the key
`filed:{reportId}:{cycle}` texts once per approval cycle.

**Approval SMS:** each approval step, the final approval and a return send the same three people one SMS, sent as
the approver: "… was approved by X and is now waiting for Y's approval", "… was fully approved by X. Please submit
the original receipts to Finance.", or "… was returned by X: "note". Please update and re-file it in ERFWeb." (a long
note is shortened to fit). The key `{event}:{reportId}:{cycle}:{step}` texts once per decision. Reject, override,
line adjustments and Finance steps are still only logged.

Configure the gitignored `appsettings.json` (template in `appsettings.Example.json`):

```json
"Lasius": { "Url": "https://…", "ApiKey": "…", "ApiKeyHeaderName": "X-API-Key", "Application": "ERMS", "TimeoutSeconds": 30 }
```

Without `Url` and `ApiKey` the API still starts and the endpoint answers 503 `sms_not_configured`.

| Outcome | HTTP | `code` |
| --- | --- | --- |
| Sent (or a retry of one already sent: `replayed: true`) | 200 `{ status, recipientCount, replayed }` | — |
| Invalid request | 400 | `validation_failed` (with `errors`) |
| User not found or inactive / report not found | 404 | `sms_user_not_found` / `sms_report_not_found` |
| No one to text has a mobile number | 422 | `sms_no_text_group` |
| SMS not configured | 503 | `sms_not_configured` |
| Lasius refused the message | 502 | `sms_gateway_failed` (may be retried with the same key) |
| Lasius did not answer | 502 | `sms_gateway_unconfirmed` (never resent under the same key) |
| Same key still sending / key reused for another message | 409 | `sms_in_progress` / `sms_idempotency_key_reused` |

Duplicate protection: a retry with the same `idempotencyKey` never texts twice. The key is claimed in
`sms_deliveries` (unique per sender) before Lasius is called. Without a key, the same message to the same user
from the same sender within a 10-minute window is treated as a retry. Once the call to Lasius starts, it
finishes even if the browser leaves the page. The message text, phone numbers, and API key are never logged,
stored, or returned. `workflow_audit` records `SmsSent`, `SmsFailed`, or `SmsUnconfirmed` with counts only.
