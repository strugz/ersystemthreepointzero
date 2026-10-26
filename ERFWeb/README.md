# ERFWeb

Web-based Expense Report (ERF) system. Standalone project — **not** part of the desktop
solution `ER System.sln` or the installer, and separate from the existing `Web/` portal.

- **Backend:** ASP.NET Core (net10.0), EF Core on **PostgreSQL** (Npgsql). Data access is
  **EF Core + LINQ only** (no stored procedures). API docs via Swagger.
- **Frontend:** Vue 3 + Vuetify + Vite + TypeScript SPA.

See `../ER System 3.0/CLAUDE.md` context and the project documentation for the full plan.

**Putting a change live?** Follow the [Deployment guide](#deployment-guide) at the end of this file.

## Change log

Newest first. Every change to ERFWeb adds a line here and updates the matching section below (and the ERFWeb
section of the root `CLAUDE.md`) in the same change.

### 2026-10-26

- **R47 Work With prints as initials** (not yet committed). Each group header reads `<location> Work w/: <initials>`:
  every Work With name becomes the first letter of each word ("Jay C Abaoag" → `JCA`), several names are joined
  by `/`, and a one-word entry is kept as typed (`JSO`). Full names no longer print on the R47. Domain
  `R47Layout.WorkWithInitials`, tests in `R47LayoutTests`.
- **Sort expense lines into the R47 order** (PR #29). The editor's **Sort** menu (date or total) saves the order
  as `sort_order` through `PUT /api/reports/{id}/lines/order`; the lines table always shows the saved order,
  grouped like the R47. No database script.
- **CRM picker day arrows and File report from the R47 preview** (PR #28). **From CRM** steps a day at a time with
  previous/next arrows inside the report period; picking an activity sets the line's date. The claimant's R47
  preview shows **File report** / **Re-file report** when the report can be filed, opening the editor's filing
  checklist (`/reports/:id?file=1`).
- **Phase 8.4–8.6** (PR #27). Daily SLA approval reminders ([SLA reminders](#sla-reminders-plan-phase-84)),
  guardrail tests without a database and the append-only `workflow_audit` (script `021`,
  [Guardrail tests](#guardrail-tests-plan-phase-85)), and the nightly backup and health monitor
  ([Backups and monitoring](#backups-and-monitoring-plan-phase-86)).

## Layout

```
ERFWeb/
  Backend/   ERFWeb.sln  ->  src/{Api,Application,Domain,Infrastructure}  +  tests/ERFWeb.Tests
             tools/  publish, deploy and service scripts, plus the nightly backup and health monitor (README.txt)
  Database/  numbered SQL scripts (NNN_*.sql) and maintenance/ reset scripts
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
| `Authentication:CrossSiteCookies` (optional, default `false`) | `true` when the SPA is on another site than the API (Render): session and antiforgery cookies become `SameSite=None; Secure`. Safari blocks such cookies |
| `Cors:AllowedOrigins` | SPA origins allowed to call the API with cookies |
| `Swagger:Enabled` | Swagger UI on/off (`false` in base, `true` in `appsettings.Development.json`) |
| `R47Report:*` | R47 form constants: company, document code, effectivity date, revision, rows per page, UTC offset, fonts, QuestPDF license tier |
| `Attachments:MaxFileSizeMb` / `MaxFilesPerReport` / `AllowedContentTypes` | Receipt upload limits (also sets the Kestrel/multipart body limit) |
| `ReferenceNumber:Prefix` / `Digits` / `UtcOffsetHours` | ERF reference number series, e.g. `ERF-2026-000142` (prefix, zero-padding, which local year a filing belongs to) |
| `Lasius:*` (optional) | Notification SMS gateway URL, API key and header (see Notification SMS) |
| `Crm:*` (optional) | CRM employee and activity lookups: `BaseUrl` (e.g. `https://<crm-host>/api2/erf/v1`), `ApiKey`, `ApiKeyHeaderName` (`X-Api-Key`), `TimeoutSeconds`. Without it the Add User CRM search answers 503, accounts save unlinked, and the line editor hides **From CRM** |
| `Smtp:*` (optional) | Mail server, sender mailbox and login for report email (see Report email) |
| `Reminders:Enabled` / `RunAt` (optional) | Daily SLA approval reminders; off unless `Enabled` is `true`, run at `RunAt` (`HH:mm`, business time, default `08:00`) on working days (see SLA reminders) |
| `Kestrel:*` (server) | Endpoint the API listens on behind nginx |
| `RateLimiting:SignInPerMinute` (optional, default 30) | Sign-in and password-change attempts allowed per client address per minute (429 `rate_limited` beyond) |
| `RateLimiting:ApiPerMinute` (optional, default 1200) | All other API requests per client address per minute; only stops runaway clients |
| `ReverseProxy:KnownProxies` (optional) | nginx's IP address(es) as Kestrel sees them. Only these may set the client address and scheme through `X-Forwarded-For` / `X-Forwarded-Proto`; with none listed, every request appears to come from nginx and the per-address limits apply to all users together |
| `Logging:File:Directory` (optional, default `logs`) | Folder for the daily log files `erfweb-yyyyMMdd.log` (30 kept, 50 MB each at most); relative to the install folder |
| `Logging:*`, `AllowedHosts` | Standard ASP.NET Core; `Logging:LogLevel` filters the console and the log files alike |

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
psql -d erfweb -f Database/014_email_deliveries.sql      # Phase 7: R47 email delivery log (sent status)
psql -d erfweb -f Database/015_security_and_schema_versions.sql # Phase 8.3: forced password change, schema_versions
psql -d erfweb -f Database/016_fare_modes.sql             # fare modes (Taxi, Grab, Jeepney, ...) on fare legs
psql -d erfweb -f Database/017_admin_only_authority.sql  # no countersign, no Business Owner: activates pending grants
psql -d erfweb -f Database/018_crm_employee_link.sql     # users.crm_employee_id: an account's link to its CRM employee
psql -d erfweb -f Database/019_user_r47_reviewed_by.sql  # users.r47_reviewed_by: an employee's own R47 REVIEWED BY
psql -d erfweb -f Database/020_expense_line_crm_activity.sql  # expense_lines.crm_activity_id: the CRM activity a line came from
psql -d erfweb -f Database/021_workflow_audit_append_only.sql # Phase 8.5: the database refuses UPDATE/DELETE/TRUNCATE of workflow_audit (9.9)
```

Run the scripts in order on a new database. `004` inserts into the `users.role` column that `006` drops, so it
cannot be run on its own after `006`; on an existing database, add test users through the administration console.

Every script is idempotent (safe to re-run) and ends with a commented rollback. Do **not** also run `dotnet ef database update`
against the same database unless you first baseline the migration history.

**Starting over with the data (keeps the administrators).** `Database/maintenance/reset_data_keep_admins.sql` runs a
`DELETE FROM` on every table and deletes every user except `admin` (`admin2` is deleted too). Only `admin` (with its System Administrator role),
its department, the PHP currency, the fare modes and `schema_versions` stay. It runs as one transaction and stops without deleting anything if `admin` is missing. Create a second administrator again from the
administration console afterwards, since an administrator cannot change their own account. It is destructive and has no rollback, so
take a `pg_dump` first:

```bash
psql -d erms_db -f Database/maintenance/reset_data_keep_admins.sql
```

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

**Audit trail is append-only (script 021).** A trigger refuses any change to or deletion of `workflow_audit` rows,
and `TRUNCATE`. The maintenance scripts that clear audit rows (`reset_data_keep_admins`, `reset_reports`,
`delete_users_keep_admin`) opt in for their own transaction with `SET LOCAL erfweb.allow_audit_purge = 'on'`; the
API never does. Any other script that must delete audit rows needs the same line.

### Guardrail tests (plan Phase 8.5)

The RBAC v0.6 guardrails 9.1–9.18 are tested without a database: the Domain rules (`tests/ERFWeb.Tests/Domain`)
and the code itself (`Architecture/GuardrailCodeTests`, named `G9_NN_…`): only the System Administrator policy may
change roles, scope and rates; Finance and the administrator read the audit trail; the audit endpoint only reads;
the Finance service has no reject; and no server code updates or deletes audit rows.

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

**Version shown to users:** the sign-in page and the bottom of every page show `ERFWeb v<version> · <commit>`. The
version is `"version"` in `package.json` (bump it for each release, e.g. `npm version minor --no-git-tag-version`);
the commit comes from `RENDER_GIT_COMMIT` on Render, otherwise `git rev-parse --short HEAD`, otherwise `local`.

## Reporting — R47 "EXPENSE REPORT" (plan Phase 3)

`GET /api/reports/{reportId}/pdf` streams the R47 controlled form as PDF (inline; add `?download=true`
for an attachment). Access: Finance/Admin see all; an employee sees their own; an approver sees reports
with an approval step assigned to them.

Pipeline: `ExpenseReportDocumentLoader` (LINQ read model) → Domain `R47Bucketing` / `R47Layout`
(column bucketing — each column carries the line total (amount × multiplier), VAT is its own memo column —
per-date "Work With" grouping — the header prints `<location> Work w/: <initials>`, each Work With name shortened to its initials (`R47Layout.WorkWithInitials`: "Jay C Abaoag" → `JCA`, names joined by `/`, a one-word entry kept as typed) so full names never print — Crystal-style pagination by height — `R47Report:DetailHeight` is the detail area in points (255, about 19 single-line rows; each wrapped line of a long client heading or particulars adds 8.2 pt; the old `RowsPerPage` key is ignored), grid text is 5.8 pt `R47Report:GridFont` (Tahoma) as on the Crystal printout, a group header is never orphaned and is repeated with its date when a group continues on the next page — and the running cash-advance reconciliation: each page carries `cash advance − prior pages' expenses`, less this page's total, giving that page's BAL DUE) → `R47Document` (QuestPDF, US Letter landscape) → `R47PdfRenderer`.

Form constants (company, document code, effectivity date, revision, rows per page, UTC offset, QuestPDF
license tier) live in the `R47Report` section of `appsettings.json` (`R47ReportOptions`). The MDMPI logo
is an embedded resource. Fidelity is checked against the reference printout: `R47PdfRendererTests`
writes `R47-sample-page1.pdf` to the test output folder for side-by-side comparison.

Footer signatories: with a two-step route the Level 1 approver prints in ENDORSED BY and the Level 2 approver
in APPROVED BY (a one-step route fills only APPROVED BY); each signature prints over the name once that step
is approved. REVIEWED BY is not an approver; the first value set wins: the employee's own (**Users → Edit →
Reviewed by (R47)**, `users.r47_reviewed_by`, script `019`), then the department's (**Master data → Departments →
Reviewed by**), then the setting `Reports.R47ReviewedBy` (default `FINANCE`) under **Rules → Expense report (R47)**. The employee's
signature prints over their full name above EMPLOYEE'S SIGNATURE.

> **License:** ERFWeb uses QuestPDF under the **Community** license (free, including commercial use)
> because MDMPI's annual gross revenue is under the USD 1M threshold. The threshold is assessed on the
> whole organisation including affiliates/parent group — re-verify at questpdf.com/license if that
> changes, and set `R47Report:QuestPdfLicense` to `Professional`/`Enterprise` if a paid tier is ever needed.

SPA: `/reports/:id/pdf` (view / print / download / open in new tab); the dashboard has a report-ID launcher.
When the claimant previews a report they can file (draft or returned), the page also shows **File report** /
**Re-file report**, which opens the editor at `/reports/:id?file=1` to run the filing checklist and confirmation.

## Authoring & filing (plan Phase 4)

Employees create, edit, and file their own ERFs; everything is EF Core + LINQ inside the serializable
`ITransactionRunner` (commit on success, rollback on any failure). Each transaction first publishes the
acting user to PostgreSQL (`set_config('app.current_user_id', …, true)`) so the `fn_record_history`
trigger stamps `record_history.changed_by`.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/reports` | My reports, paged (`page`, `pageSize`, `sort`, `direction`, `status`, `search`) |
| `POST /api/reports` · `GET/PUT/DELETE /api/reports/{id}` | Create / read / update header + cash advance / delete (never-filed only) |
| `POST /api/reports/{id}/lines` · `PUT/DELETE …/lines/{lineId}` | Expense lines with meal / fare-leg items. Every fare leg saved names its fare mode (`fareModeId`, plus `fareModeDetail` for modes such as Others); legs saved before script 016 have none until the line is saved again |
| `PUT /api/reports/{id}/lines/order` | Body `{ lineIds }`: every active line once, in the order the R47 prints them (saved as `sort_order`). A list that no longer matches the report's lines → 409. The editor's **Sort** menu (date or total) calls it |
| `GET /api/lookups/fare-modes` | Active fare modes for the fare-leg picker, in sort order |
| `POST /api/reports/{id}/cash-advances` · `PUT/DELETE …/cash-advances/{lineId}` | Rows of the CASH ADVANCE RECEIVED block (Liquidation of Cash Advance only); the R47 reconciles against their total |
| `GET/POST /api/reports/{id}/attachments` · `GET …/{aid}/content` · `DELETE …/{aid}` | Receipt scans (`multipart/form-data`, field `file`); stored as `bytea` in `receipt_attachments`, not on disk. The browser shrinks JPEG/PNG photos first (`src/shared/images.ts`: longest side 1600 px, JPEG 80 %, EXIF orientation applied, PDFs untouched, original kept if not smaller) |
| `POST /api/reports/{id}/file` | File / re-file → `ForApproval`; snapshots the approval route into `approval_steps` |
| `POST /api/reports/{id}/reopen` | Pull a `ForApproval`/`Approved` report back to `New` (supersedes the cycle) |
| `GET /api/lookups/work-with` · `my-rates` · `my-filing-readiness` | Editor lookups (`crmActivitiesAvailable` turns on **From CRM**) |
| `GET /api/reports/{id}/crm-activities?date=YYYY-MM-DD` (report owner) | The owner's CRM activities on the line's date, every status, Completed first and marked when already claimed (see CRM activities on expense lines) |

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
Filing hands a `ReportFiledNotification` to `IApprovalNotifier`, which logs it, texts it (Lasius) and emails it
with the R47 attached (SMTP). Delivery runs after the filing commits and can never roll it back.

SPA: **My reports** (`/reports`, status/search filters, deep-linkable `?status=`), **New report**
(`/reports/new`), and the **editor** (`/reports/:id`: header + cash advance, grouped expense-lines table,
add/edit line dialog with meal chips, fare legs (each with its mode: Taxi, Grab, Angkas, Jeepney, ...) and per-diem preview, receipts panel, approval timeline,
filing checklist, file / re-file / reopen / delete with confirmations).

## Auth (plan Phase 0)

Cookie session (`ERFWeb.Session`) + antiforgery (`X-CSRF-TOKEN`). Endpoints:
`GET /api/auth/antiforgery`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`,
`POST /api/auth/password`, `GET /api/health`. Passwords verify with PBKDF2; the administration console (below) creates
users, or seed one with a `Pbkdf2PasswordHasher.Hash` value. The session's Role claims are the account's **Active**
grants in `user_roles` plus the implicit `Employee`.

**Sign-in lockout (Phase 8.3):** five failed sign-ins in a row lock that username for 15 minutes (`login_security`;
failures older than 15 minutes no longer count). Unknown usernames are counted the same way, so a lockout never reveals
whether an account exists. While locked, `POST /api/auth/login` answers **423** `account_locked` with how many minutes
are left, without checking the password. A successful sign-in clears the count; an administrator's password reset
unlocks the account. Each lockout writes `Auth.LockedOut` (username tried, never the password) to `workflow_audit`.

**Rate limits:** per client address, `RateLimiting:SignInPerMinute` (default 30) for sign-in and password change and
`RateLimiting:ApiPerMinute` (default 1200) for everything else. Beyond them: **429** `rate_limited` with `Retry-After`.
The client address comes from nginx's `X-Forwarded-For` only when nginx is listed in `ReverseProxy:KnownProxies`.

**Password change:** `POST /api/auth/password` `{ currentPassword, newPassword }` (signed in). The new password needs at
least 8 characters, must differ from the username and from the current one; a wrong current password is a 400 on
`currentPassword`. Success re-issues the session cookie and writes `Auth.PasswordChanged`. SPA: **Change password**
in the account menu (`/account/password`).

**Must change password (script `015`):** a password set by an administrator (new account or reset) sets
`users.must_change_password`. Sign-in still works, but the SPA goes straight to the change-password page and the API
answers every other call with **403** `password_change_required` until the user picks their own. Existing accounts are
not affected (the column defaults to `false`).

**Security headers** on every API response: `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`,
`Content-Security-Policy: frame-ancestors 'self'` plus the `Cors:AllowedOrigins`, and `Cache-Control: no-store` unless
the endpoint set its own. HSTS is left to nginx (it serves other applications on the same host).

## Administration & accounts (plan Phase 1, RBAC v0.6)

Roles are additive rows in `user_roles` (script `006`): `Employee` (implicit, everyone), `SignatoryLevel1`,
`SignatoryLevel2`, `Finance`, `ExpenseVerifier`, and `SystemAdministrator`. The old
`users.role` column is migrated by `006` (`Manager` → `SignatoryLevel1`, `Admin` → `SystemAdministrator`) and dropped.

Rules enforced by the API (Domain `RoleRules` / `AccountRules`, unit-tested):

- **No countersign.** A grant is `Active` as soon as a System Administrator saves it. Dual approval (9.8) and the
  `BusinessOwner` role were dropped by script `017`, which activated any grant still pending and deleted Business
  Owner grants (both audited). The `countersigned_*` and `is_bootstrap` columns remain as history.
- **No self-change (9.7).** An administrator cannot edit, deactivate, reset, or change roles on their own account.
  One administrator (`admin`) is enough; `admin2` from `006` is optional and can be deactivated.
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
| `POST /api/admin/users/{id}/roles` · `POST …/roles/{role}/revoke` (SystemAdministrator) | Grant, revoke |
| `GET/POST/PUT /api/admin/master-data/{departments,cost-centers,currencies,sites,fare-modes}` · `GET …/expense-categories` | Master data (deactivate, never delete). Fare modes replace the legacy `dbo.tbFare` list |
| `GET/PUT /api/admin/settings` | Workflow rules (Level 2 threshold, SLAs, escalation), business rules (filing cut-off, receipt threshold, cooling period), notification switches, account email domains; stored in `app_settings` |
| `GET /api/admin/crm/employees?search&includeInactive` · `GET …/{employeeId}` (SystemAdministrator) | CRM employee lookup for the user form (see CRM employee link) |
| `GET /api/admin/audit?event&actorUserId&from&to` (SystemAdministrator, Finance) | Append-only audit trail |

First-time setup (plan flow 1): sign in as `admin`, create accounts and master data, set the rules, grant the
signatory roles, then give each signatory a scope (Scope page) so reports route to them.
A department's own R47 REVIEWED BY is edited on **Master data → Departments** (stored in
`department_signatories.reviewer_initials`; blank uses the Rules default). The endorser and approver initials
in that table are only a fallback before filing and are still maintained by SQL.

### CRM employee link

The CRM holds no employee email, so an account is tied to the CRM by the CRM **employee ID** (e.g. `CNT-001`),
stored in `users.crm_employee_id` (script `018`). The later CRM activity endpoints (visits and service work to
claim on expense lines) take this ID. The data contract with the CRM team is
`Document/CRM-API-Integration-Spec.md`; their side is the "CRM Employee Lookup API for ERFWeb".

- **Picking.** On New user and Edit, the CRM link field searches the CRM by name, initials or employee ID
  (2 characters or more; "Include people who have left" adds leavers). The admin picks one person; the stored
  ID shows in the read-only **CRM employee ID** box beside it. Names are never matched automatically.
- **Prefill.** Picking fills **Full name** and **Mobile no.** from the CRM (`fullName`, `mobileNumber` = CRM
  `CNTNUM`) only when those fields are empty. Email, department and the other fields are entered by hand.
- **Rules (`UserAdminService`).** One CRM employee links to one account (also a unique index). On create, and on
  edit when the link changes, the server asks the CRM whether the ID exists; an ordinary profile edit never calls
  the CRM. Clearing the field and saving removes the link. The ID is in the `Admin.UserCreated` /
  `Admin.UserUpdated` audit rows.
- **Server to CRM.** `CrmEmployeeDirectory` calls `GET {Crm:BaseUrl}/employees?search=` and
  `/employees/{employeeId}` with the key in `X-Api-Key`. It logs only the HTTP status, never the search text,
  the key or the response. The browser never calls the CRM.
- **Errors.** 400 when the search is shorter than 2 characters; 503 `crm_not_configured` without the `Crm`
  section; 503 `crm_unavailable` when the CRM refuses (wrong key, 5xx), times out or answers unreadable JSON.
  The account can always be saved without a link.

### CRM activities on expense lines

An employee whose account is linked to the CRM can pre-fill an expense line from a completed CRM activity: a
Sales itinerary visit (`ITI-…`) or a PS/TSG service report (`SR-…`), including reports where another engineer
listed them as work-with. The CRM supplies what, where, when and with whom; the employee still picks the
category and enters the amount, Invoice/OR, VAT and receipts. Their side is the CRM team's "CRM Employee and
Activity API for ERFWeb".

- **Picking.** **From CRM** in the line editor (shown when `my-filing-readiness` reports
  `crmActivitiesAvailable`) lists all the employee's activities on the line's date: Completed ones first, which
  can be picked, then Open and Cancelled ones, shown greyed out so the day is complete (one CRM call per status).
  The previous/next day arrows step one day at a time inside the report period (the API refuses days outside
  it), so an empty day no longer means closing the picker. Picking one
  fills Date (`activityDate`), Location (`customer.name`), Work With (the other people,
  by their ERFWeb name where linked), Service no., Instrument and Serial no., and on a Transportation line the
  first fare leg (`travel.origin` → `travel.destination`). Particulars keeps the category default (Meals,
  Transportation, Toll, Parking…) and stays editable. The line keeps the link (`expense_lines.crm_activity_id`,
  script `020`); removing the chip makes it an ordinary typed line. Typing lines by hand always works.
- **Rules (`CrmActivityClaimCheck`).** When a line gains or changes its link, the server asks the CRM
  (`GET /activities/{activityId}`) that the activity exists, is Completed, belongs to the employee (owner or
  companion), and covers the line's date (any day from its start to its completion for multi-day work). One
  activity may fill several lines of one report (a meal and a fare from one visit), but not lines of another of
  the employee's reports unless that report was Rejected. A line that keeps its link saves without calling the CRM.
- **Server to CRM.** `CrmActivityDirectory` calls `GET {Crm:BaseUrl}/activities?employeeId=&dateFrom=&dateTo=&status=`
  with the account's own `crm_employee_id`; it logs only the HTTP status. The browser never calls the CRM.
- **Errors.** 400 when the date is outside the report period; 409 when the CRM no longer knows the
  linked employee ID (relink the account); 503 `crm_not_configured` / `crm_unavailable` as for the lookup.
- **Not used yet.** `distanceKm`, `isOutOfTown` and the customer branch/city are always null in the CRM for
  now; the "claimed" callback to the CRM is not built.

### Account email domains

The Email field on the user form is a name plus a domain dropdown. The domains are the setting
`Accounts.EmailDomains` (default `marsmandrysdale.com`), edited under **Settings → Account email domains**: type a
domain and press Enter; the first one is the default for new accounts. `EmailDomainRules` trims, lower-cases,
drops a leading `@` and duplicates, and refuses an invalid domain, an empty list or more than 20. Typing a full
address (with `@`) in the name box keeps it as typed, for addresses outside the list; the server accepts any valid
email. No database script: a missing row falls back to the default.

## Scope & rate profiles (plan Phase 2)

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
A planned change adds a version and closes the open one the day before, so a report always uses the rate in force
on its expense date. **Edit** corrects one version in place (amount, rule, receipt, cap, dates; category and meal
stay) with a required reason, audited as `Rates.LineCorrected`; its dates may not overlap another version. Filed
reports keep their own amounts, and the old values stay in `record_history`. A profile reaches a person by assignment, resolved Individual > Grade >
Department > Company on the expense date. **Edit** on an assignment (`PUT /api/rates/assignments/{id}`) corrects
its target or dates in place (the profile stays) with a required reason, audited as `Rates.AssignmentCorrected`;
its period may not overlap another assignment for the same target, and it needs `backdateOverride` only when it
changes a day before today. A change that starts before today and replaces what is in force (a new version, a correction, an assignment that ends another for the same target) needs `backdateOverride` and a reason; the first rate line for a category/meal and the first assignment for a target may start in the past freely, e.g. to cover claims from before go-live.
`GET /api/lookups/my-rates?date=` gives the line editor the rates in force on that date. A person assigned a
profile gets only that profile's lines; categories it does not define are zero.

**Authority** (Domain `EntitlementAuthority`). Scope at both levels and rates are administration settings: only a
System Administrator changes them. The administrator holds no signatory role, so never sets scope they hold; the
one self-interest guard left is that they cannot name themselves in a rate assignment (9.12). Grade, department
and company targets stay open because the only administrator may share them. Older rows created under the
previous Level 2 / Business Owner rules may still show `isBootstrap`. Signatories and Finance read scope and rates;
every change is in the audit trail Finance reviews.

| Endpoint (policy) | Purpose |
| --- | --- |
| `GET /api/scope/assignments` · `GET …/{id}` · `GET /api/scope/coverage?date` (signatories, Finance, System Administrator) | Read scope and who covers whom |
| `POST /api/scope/assignments` · `PUT …/{id}` (System Administrator) | Create; change members, limit, end date, active |
| `GET /api/rates/profiles` · `GET …/{id}` · `GET /api/rates/assignments` · `GET /api/rates/resolve?userId&date` | Read profiles, versions, assignments, what applies to a person on a date |
| `POST/PUT /api/rates/profiles` · `POST …/{id}/lines` · `PUT …/{id}/lines/{lineId}` · `POST /api/rates/assignments` · `POST …/{id}/end` (System Administrator) | Maintain profiles, add or correct rate versions, assign and end |
| `GET /api/lookups/departments` (any signed-in user) | Department picker |

Change notifications to affected employees (9.14) go through `IEntitlementChangeNotifier`, which still only logs:
the scope/rate change email is not built yet.

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
| `GET /api/approvals/export?view&search` (Level 1, Level 2) | The whole inbox view as CSV |
| `GET /api/finance/queue/export?status&openOnly&search` (Finance) | The queue for the same filters as CSV |
| `GET /api/finance/aging` · `GET /api/finance/aging/export?format=Csv\|Pdf` (Finance) | Open reports by days since final approval (0-7, 8-15, 16-30, 31-60, over 60) |
| `GET /api/summaries/expenses?from&to&scope=Mine\|Approvals\|All&departmentId&approvedOnly` · `/expenses/export?…&format=Csv\|Pdf` (any user; scope checked) | Expense summary by expense date: Mine for everyone, Approvals for signatories, All for Finance. CSV is one row per line; PDF has employee totals by category, then the lines. Exports are capped at 10,000 rows and audited as `ExportGenerated` |

`GET /api/reports/{id}` now also returns `review` (what the viewer may do as an approver, from the same rules)
and `finance` (receipt status). SPA screens: **Approvals** (`/approvals`) and the review page (`/review/{id}`)
for signatories, **Finance queue** (`/finance`) for Finance. Workflow notifications go through
`IApprovalNotifier.NotifyWorkflowEventAsync`, which logs every event and sends the SMS and email described below.

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

## Delegation, signatures, summaries and exports (plan Phase 7)

**Approval delegation** (script `013`): a signatory names a delegate of the same level or higher for a date range
(at most 90 days); while it is in force the delegate approves in their place, and the step reads "by Delegate on
behalf of Signatory". A Level 2 may assign a delegate for an absent Level 1 they are over (the scope Level 2 of that
Level 1's account, or routed after them on a pending report). Only approving is delegated; scope, rates and
entitlement never are. A delegate never fills a second step on a report they already approve.

| Endpoint (Level 1, Level 2) | Purpose |
| --- | --- |
| `GET /api/delegations` | Delegations I gave, received, or assigned for a Level 1 |
| `GET /api/delegations/options` | Who can stand in, and the Level 1s I may assign for |
| `POST /api/delegations` · `POST /api/delegations/{id}/cancel` | Create or cancel one |

SPA: **Delegations** (`/delegations`).

**Approver signatures on the R47:** the R47 prints each routed approver's signature on file once their step is
approved (ENDORSED BY for Level 1, APPROVED BY for Level 2); a delegated step prints the delegate's name and
signature, never the absent signatory's. The System Administrator loads signatures in the user console, with a
preview.

**Summaries and exports:** the endpoints are listed under Approvals & Finance above (expense summary as CSV/PDF,
approval inbox and Finance queue CSV, Finance aging). SPA: **Summary** page and export buttons on the Approvals and
Finance pages. No database script.

**Health and theme:** `GET /api/health` (anonymous) returns
`{ status, service, version, database, schemaVersion, requiredSchemaVersion }`: 200 when the database answers within 5
seconds and has every script this build needs; otherwise 503 `degraded` with `database` `unreachable` or
`schema_outdated` (`schemaVersion` is the highest script recorded in `schema_versions`, `requiredSchemaVersion` the one
the build needs). `version` is `Version` in `Directory.Build.props` plus the build commit (e.g. `0.1.0+edadad9`). Used by monitoring, deploy smoke tests and the dev proxy; the SPA has a
light / dark / system theme switch.

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
line adjustments and Finance steps send no SMS (a rejection is emailed; see Report email).

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

## Report email (R47 over SMTP)

Script `014` adds `email_deliveries`, the report's email sent status. `POST /api/reports/{id}/email` takes
`{ idempotencyKey? }` and emails the R47 PDF of a filed report to its claimant, its Level 1 and Level 2 signatories
on the current approval cycle, and the sender: anyone who may view the report may send it, and the server picks
the addresses from `users.email` (a shared mailbox gets one copy). `GET /api/reports/{id}/email` lists the latest
20 emails about the report (kind, status, recipient count, sent by, times). The R47 PDF page has an **Email** button
and shows the last email.

**Workflow email:** `EmailApprovalNotifier` wraps the SMS notifier, so the employee hears at every stop. Filing,
re-filing, each approval step, the final approval, a return and a rejection email the employee and the Level 1 and
Level 2 signatories, with the R47 rendered after the event attached and the approver's remarks quoted. Keys
`filed:{reportId}:{cycle}` and `{event}:{reportId}:{cycle}:{step}` email once per event. It runs after the
workflow commits and never fails it. Override, line adjustments and Finance steps send no email.

Configure the gitignored `appsettings.json` (template in `appsettings.Example.json`); `Security` is `Auto`,
`None`, `StartTls` or `SslOnConnect`, and `Username` may be empty for a relay that needs no login:

```json
"Smtp": { "Host": "…", "Port": 587, "Security": "StartTls", "Username": "…", "Password": "…", "FromAddress": "erfweb@…", "FromName": "ERFWeb", "TimeoutSeconds": 30 }
```

Without `Host` and `FromAddress` the API still starts, the endpoint answers 503 `email_not_configured`, and
workflow emails are skipped (logged).

**From:** every email is from the configured mailbox (`FromName` <`FromAddress`>), which must be the account
`Username` logs in as, or one it may send as. The body names who sent or acted.

| Outcome | HTTP | `code` |
| --- | --- | --- |
| Sent (or a retry of one already sent: `replayed: true`) | 200 `{ status, recipientCount, replayed }` | — |
| Invalid key | 400 | `validation_failed` (with `errors`) |
| Not allowed to view the report | 403 | — |
| Report not found | 404 | `email_report_not_found` |
| Draft not filed yet / no one has an email address | 422 | `email_report_not_filed` / `email_no_recipients` |
| Email not configured | 503 | `email_not_configured` |
| Mail server refused (or connect/login failed) | 502 | `email_gateway_failed` (may be retried with the same key) |
| Mail server did not confirm | 502 | `email_gateway_unconfirmed` (never resent under the same key) |
| Same key still sending / key reused for another email | 409 | `email_in_progress` / `email_idempotency_key_reused` |

Same duplicate protection as SMS: the key is claimed in `email_deliveries` (unique per sender) before SMTP is
called; without a key, a second send of the same report within 10 minutes is a retry. Addresses, subject, body
and the PDF are never logged or stored. `workflow_audit` records `EmailSent`, `EmailFailed` or `EmailUnconfirmed`
with the kind and counts only.

## SLA reminders (plan Phase 8.4)

A background job inside the API service sends approval reminders once a working day (Monday to Friday) at
`Reminders:RunAt` in the business time zone (`ReferenceNumber:UtcOffsetHours`). It is off unless
`Reminders:Enabled` is `true`, so a developer PC pointed at a shared database never sends any:

```json
"Reminders": { "Enabled": true, "RunAt": "08:00" }
```

The timing is the System Administrator's **Settings** (Domain `ApprovalReminderRules`, no holiday calendar):

* A step's clock starts when it becomes the approver's turn (the filing, or the previous step's approval) and
  counts working days.
* After **SLA days** for its level (`Workflow.SlaDaysLevel1` / `SlaDaysLevel2`, default 3) the approver is
  reminded, then every 2 working days until they act. Their delegate, when one is in force, gets it too. An SLA
  of 0 turns reminders off for that level.
* A Level 1 step still waiting **Escalation after days** (`Workflow.EscalationAfterDays`, default 3) past its SLA
  is reported once to the report's Level 2, who can follow up, override, or assign a delegate.
* Each reminder goes by email (R47 attached) and SMS to the people it is for only, never the claimant.

Each reminder is claimed once in `reminder_deliveries` (unique per report, step, reminder number, channel and
audience: `Approver` or `Level2`) before it is sent, so a restart or a second run that day sends nothing twice.
A reminder the gateway refused (`Failed`) is retried on the next run while it is still the current one; one with
no answer (`Unconfirmed`) is not. Nobody with an address or number on file: `Skipped` with `failure_code`
`NoContact`. A channel whose `Smtp` or `Lasius` section is missing is left alone until it is configured. A service
started after `RunAt` on a working day runs at once. `workflow_audit` records `ReminderSent`, `ReminderFailed` or
`ReminderUnconfirmed` with counts only; the log has one line per run (`SLA reminder run: …`). No new script: the
table is in `001`.

## Deployment guide

Step-by-step instructions for putting a change live. Read **How it is set up** once; after that, find your kind
of change in **Which steps do I follow?** and do only those steps.

### How it is set up

| Part | Where it runs | How it is updated |
| --- | --- | --- |
| **Frontend** (Vue SPA) | Render static site **https://ersystemthreepointzero.onrender.com** | Render builds it from the `master` branch of GitHub |
| **Backend** (ERFWeb API) | Windows server, folder `C:\inetpub\wwwroot\erf_web-app`, Windows Service **ERFWeb.Api** | `Publish-ErfWeb.cmd` on your PC, then `Deploy-ErfWeb.cmd` on the server |
| **Public API address** | nginx at **https://sr.mdmpi.com.ph/erf** → the service at `http://192.168.4.206:5080/5081` | nginx forwards `/erf/*` to the API's `/api/*`, so the health check is `/erf/health` |
| **Database** | PostgreSQL `erms_db` | SQL scripts in `ERFWeb/Database/`, run by hand |
| **Server settings** | `C:\inetpub\wwwroot\erf_web-app\appsettings.json` on the server only | Edited by hand on the server; never in git, never in a release |

The backend and the frontend are deployed separately, and Render deploys as soon as `master` changes. Always put
the **backend live first**, so the new screens never call an API that does not exist yet.

### Which steps do I follow?

| What you changed | Steps |
| --- | --- |
| Backend code (`ERFWeb/Backend/...`) | [A](#a-before-you-start) → [C](#c-build-the-backend-release-on-your-pc) → [D](#d-deploy-the-backend-on-the-server) → [F](#f-check-that-it-works) |
| Frontend code (`ERFWeb/Frontend/...`) | [A](#a-before-you-start) → [E](#e-deploy-the-frontend-render) → [F](#f-check-that-it-works) |
| Backend and frontend | [A](#a-before-you-start) → [C](#c-build-the-backend-release-on-your-pc) → [D](#d-deploy-the-backend-on-the-server) → [E](#e-deploy-the-frontend-render) → [F](#f-check-that-it-works) |
| A new database script (`ERFWeb/Database/NNN_*.sql`) | [A](#a-before-you-start) → [B](#b-update-the-database-only-when-there-is-a-new-script) → then the backend steps |
| Server settings only (`appsettings.json` on the server) | [Change a server setting](#change-a-server-setting) |
| Nightly backup and health monitor (once per server, or to change its settings) | [Backups and monitoring](#backups-and-monitoring-plan-phase-86) |

### A. Before you start

1. **Verify on your PC.** From `ERFWeb/Backend`: `dotnet build ERFWeb.sln`, `dotnet test ERFWeb.sln`,
   `dotnet format ERFWeb.sln --verify-no-changes`. From `ERFWeb/Frontend/erfweb-client`: `npm run lint`,
   `npm run build`.
2. **Commit your changes.** The release name, `/erf/health` and the page footer all show the last commit, so an
   uncommitted change cannot be identified once it is live.
3. **For a real release, raise the version** in both places, keeping them equal:
   `<Version>` in `ERFWeb/Backend/Directory.Build.props` and `"version"` in
   `ERFWeb/Frontend/erfweb-client/package.json`.
4. **New setting?** If `appsettings.Example.json` gained a key, add it to the server's `appsettings.json` before
   deploying (see [Change a server setting](#change-a-server-setting)). A missing required key stops the API.

### B. Update the database (only when there is a new script)

1. **Back up first** (pgAdmin → right-click `erms_db` → Backup, or `pg_dump`).
2. Run each new script in `ERFWeb/Database/` **in number order**, once, against `erms_db` (pgAdmin Query Tool, or
   `psql -d erms_db -f Database/NNN_name.sql`). Scripts are safe to re-run.
3. Each script ends with a commented **Rollback** block; keep it at hand.
4. Never run `dotnet ef database update` against this database.
5. **Run the script before deploying the backend that needs it.** Since script `015`, each script records itself in
   `schema_versions`, and the API refuses to report healthy (`"database":"schema_outdated"`) while the database is
   older than it needs, so a backend deployed too early is rolled back by `Deploy-ErfWeb.cmd` instead of failing for
   users.

**Writing a new script:** number it after the last one, make it idempotent, end it with a commented Rollback block, and
before `COMMIT;` record it:
`INSERT INTO schema_versions (version, name) VALUES (16, '016_name') ON CONFLICT (version) DO NOTHING;`
When the backend code depends on it, raise `SchemaVersion.Required` in
`Backend/src/ERFWeb.Infrastructure/Persistence/SchemaVersion.cs` to that number in the same change. A script the code
does not depend on (such as `021`, the append-only audit trail) leaves it as it is, so it can be run before or after
a deploy. A script that must delete `workflow_audit` rows needs `SET LOCAL erfweb.allow_audit_purge = 'on';` right
after `BEGIN;` (see the maintenance scripts).

### C. Build the backend release on your PC

1. Double-click **`ERFWeb\Backend\tools\Publish-ErfWeb.cmd`**.
2. When it finishes it prints the folder it made, for example:
   ```
   ERFWeb\releases\ERFWeb-Api-0.1.0-b1b0da4
       ERFWeb-Api.zip              every file of the API (no appsettings.json)
       release.txt                 the version it contains, e.g. 0.1.0+b1b0da4
       Deploy-ErfWeb.cmd           double-click this on the server to update it
       Install-ErfWebService.cmd   first install on a new server only
       appsettings.Example.json    template for the server's appsettings.json
       HOW-TO-DEPLOY.txt           these steps in short, for whoever is at the server
       *.ps1                       the scripts the .cmd files run
   ```
   The folder holds everything the server needs; nothing has to be copied from the repo by hand.
3. A warning about uncommitted changes means step A2 was skipped; commit and run it again.

### D. Deploy the backend on the server

1. **Copy the whole release folder** from step C to the server, anywhere (for example `C:\inetpub\erf-releases\`).
2. Open it and **double-click `Deploy-ErfWeb.cmd`**. Click **Yes** on the administrator prompt.
3. Watch the window. It:
   1. stops the ERFWeb.Api service (ERFWeb is unavailable for about half a minute);
   2. backs up the current version to `C:\inetpub\wwwroot\erf_web-app-backups\<date-time>` (last 3 kept);
   3. extracts the new version over the app folder, keeping the server's `appsettings.json`;
   4. starts the service and waits for `/api/health` to report the new version with database `ok`.
4. The last line tells you the result:
   * **Green "Deployed: version …"**: done. Go to step F.
   * **Yellow "Rolled back to version …"**: the new version did not start, so the previous one was put back and
     ERFWeb keeps working. Look at the newest file in `C:\inetpub\wwwroot\erf_web-app\logs\` to see why (see
     [Find an error](#find-an-error)).
   * **Red**: even the previous version is not healthy. See [Troubleshooting](#troubleshooting) now.

Never copy single DLLs by hand: a missed file stops the API (`Could not load file or assembly ...`).

### E. Deploy the frontend (Render)

1. Merge your branch into `master` on GitHub.
2. Render builds and publishes the site automatically. If it does not, open the Render dashboard → the
   `ersystemthreepointzero` static site → **Manual Deploy → Deploy latest commit**.
3. Only when you changed a Render **environment variable**, use **Manual Deploy → Clear build cache & deploy**:
   Vite bakes `VITE_*` values in at build time.

Render settings for reference: root directory `ERFWeb/Frontend/erfweb-client`, build command
`npm ci && npm run build`, publish directory `dist`, rewrite `/*` → `/index.html`, environment variable
`VITE_API_BASE_URL=https://sr.mdmpi.com.ph/erf`.

### F. Check that it works

1. **https://sr.mdmpi.com.ph/erf/health** shows
   `{"status":"ok","service":"ERFWeb.Api","version":"<new version>","database":"ok"}`.
2. **https://ersystemthreepointzero.onrender.com**: the footer at the bottom of the sign-in page shows the new
   version (`ERFWeb v0.1.0 · <commit>`). Press **Ctrl+Shift+R** if it still shows the old one.
3. **Sign in** (Chrome or Edge) and try the feature you changed.

### Change a server setting

1. On the server, open `C:\inetpub\wwwroot\erf_web-app\appsettings.json` in Notepad and make the change. Keep the
   commas and quotes valid JSON. `appsettings.Example.json` next to it lists every key.
2. In an administrator PowerShell: `Restart-Service ERFWeb.Api`.
3. Check https://sr.mdmpi.com.ph/erf/health (step F1).

Settings that matter for the live site:

| Setting | Live value / note |
| --- | --- |
| `Cors:AllowedOrigins` | `https://ersystemthreepointzero.onrender.com` (the Render address; no `localhost` on the server) |
| `Authentication:CrossSiteCookies` | `true` while the frontend is on `onrender.com`; `false` once it moves to a `mdmpi.com.ph` address |
| `Kestrel` | the address the service listens on (`http://192.168.4.206:5080/5081`), which nginx forwards to |
| `Smtp`, `Lasius`, `Crm` | email, SMS and the CRM employee and activity lookups; without them those features are skipped, not broken |
| `Reminders` | `Enabled: true` and `RunAt` (e.g. `08:00`) on the server only, for the daily SLA reminders |
| `Logging:File:Directory` | where log files go (default `logs`) |
| `ReverseProxy:KnownProxies` | nginx's IP address as the server sees it (ask IT), e.g. `["192.168.4.x"]`; until it is set, rate limits count all users together |
| `RateLimiting` | `SignInPerMinute` (30) and `ApiPerMinute` (1200); raise them if many users share one address and see 429 |

**For IT (nginx and Render):**

* nginx should pass the client address and scheme to the API:
  `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;` and `proxy_set_header X-Forwarded-Proto $scheme;`.
  Then put nginx's address in `ReverseProxy:KnownProxies`.
* HSTS belongs on nginx for `sr.mdmpi.com.ph` (`add_header Strict-Transport-Security "max-age=31536000" always;`),
  once every application on that host is HTTPS-only.
* Optional, Render static site → **Headers**: `/*` with `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: strict-origin-when-cross-origin` and `X-Frame-Options: DENY`.

### Go back to the previous version

* **Backend:** a failed deploy rolls back by itself. To go back later, double-click `Deploy-ErfWeb.cmd` in the
  **previous release folder**. (Copies of the last 3 versions are also in `C:\inetpub\wwwroot\erf_web-app-backups\`.)
* **Frontend:** Render dashboard → the static site → **Events** → pick the previous deploy → **Rollback**.
* **Database:** run the commented Rollback block at the end of the script, only after the backend that needs the
  script has been rolled back.

### Backups and monitoring (plan Phase 8.6)

Two Windows scheduled tasks on the API server, set up once with `Install-ErfWebMaintenance.cmd` from a release
folder (fill in the SETTINGS lines at its top first; `Backend/tools/README.txt` has the step-by-step guide) or `Install-ErfWebMaintenance.ps1` for the other options. Both read the
database login and the `Smtp` mailbox from the server's `appsettings.json`, so no password is stored twice, and
email alerts to `-AlertTo` (default jca.mdmpi@marsmandrysdale.com).

| Task | When | What it does |
| --- | --- | --- |
| **ERFWeb Database Backup** (`Backup-ErfWebDatabase.ps1`) | every night at 01:00 | `pg_dump` (custom format) of the database to `D:\ERFWeb-Backups\erms_db_yyyyMMdd_HHmm.dump`; checks the file with `pg_restore --list` (whole archive readable, table data present, `workflow_audit` included); copies it to the network share (`-CopyTo`) and checks the copy's size; deletes backups older than 14 days in both places. Log: `D:\ERFWeb-Backups\backup-log.txt`. Emails on any failure |
| **ERFWeb Health Check** (`Test-ErfWebHealth.ps1`) | every 5 minutes | Calls `https://sr.mdmpi.com.ph/erf/health` (nginx, API and database together) and emails "ERFWeb is DOWN" after 2 failed checks in a row, then "back up" when it recovers; also emails when the newest backup is older than 26 hours. Log (changes only): `C:\ProgramData\ERFWeb\health-log.txt` |

**Before installing:**

* PostgreSQL runs on another server, so install the **PostgreSQL command line tools** on the API server (the
  PostgreSQL installer's "Command Line Tools" component), the **same major version as the database server or
  newer**. The script finds them in `C:\Program Files\PostgreSQL\<version>\bin`, or pass `-PgBin`.
* Ask IT for the network share. The tasks run as `SYSTEM` (the share then sees the server's computer account,
  e.g. `MDMPI\SERVERNAME$`, which needs write access) or as a service account given with `-RunAsUser`.
* Check that `D:` exists, or pass `-BackupDir`.

**After installing:** run `Start-ScheduledTask -TaskName 'ERFWeb Database Backup'` in an administrator PowerShell,
then read `D:\ERFWeb-Backups\backup-log.txt`: the last line should be `Backup OK: …` and the share should hold the
same file. Running the installer again replaces both tasks; `-Uninstall` removes them and keeps the backups.

**Limits:** the monitor runs on the API server, so it cannot report that whole server being off (IT's own
monitoring should). Email goes through `System.Net.Mail`, which supports STARTTLS (`Security: StartTls`, as the
company mail server uses) but not `SslOnConnect`.

**Restoring a backup** (only on purpose; this replaces data). Into a new, empty database first, to look before
replacing anything:

```bash
createdb -h <db-host> -U <user> erms_db_restored
pg_restore -h <db-host> -U <user> -d erms_db_restored --no-owner D:\ERFWeb-Backups\erms_db_20261026_0100.dump
```

To put a backup back into `erms_db` itself, stop the API first (`Stop-Service ERFWeb.Api`), take a fresh backup of
the current state, then `pg_restore --clean --if-exists --no-owner -d erms_db <file>`, and start the API again.

### Manage the service

The backend runs as the Windows Service **ERFWeb.Api**: it starts with Windows and restarts itself if it crashes.
Manage it in **Services** (`services.msc`, "ERFWeb API") or in an administrator PowerShell:

```powershell
Get-Service ERFWeb.Api
```
```powershell
Restart-Service ERFWeb.Api
```
```powershell
Stop-Service ERFWeb.Api
```
```powershell
Start-Service ERFWeb.Api
```

**First-time install (once per server; already done on the live server).**

1. Stop any console copy of the API (Ctrl+C in its window).
2. Create `C:\inetpub\wwwroot\erf_web-app\appsettings.json` from the release folder's `appsettings.Example.json`
   with the server's values.
3. Copy a release folder to the server and double-click **`Install-ErfWebService.cmd`** in it (click **Yes**). It
   extracts `ERFWeb-Api.zip` into `C:\inetpub\wwwroot\erf_web-app`, registers and starts the service, and checks
   `/api/health`.

The same from PowerShell: `Install-ErfWebService.ps1 -InstallPath C:\inetpub\wwwroot\erf_web-app -ReleaseZip
<release folder>\ERFWeb-Api.zip`. `Backend/tools/Install-ErfWebService.ps1` runs the service as the virtual account `NT SERVICE\ERFWeb.Api` (read
access to the app folder, write access to `logs\` only), sets restart on failure (5 s, 5 s, 30 s), registers the
event log source, starts the service and checks `/api/health`. `-Uninstall` removes the service.

### Find an error

Every error the website shows has a **correlation id** (in the error response and the `X-Correlation-ID` header).
Every log line written for that request carries the same id.

* Log files: `C:\inetpub\wwwroot\erf_web-app\logs\erfweb-yyyyMMdd.log` (one per day, 30 kept).
* Warnings and errors also appear in **Event Viewer → Windows Logs → Application**, source `ERFWeb.Api`.
* Search the logs for an id:
  ```powershell
  Select-String -Path C:\inetpub\wwwroot\erf_web-app\logs\*.log -Pattern '<correlation id>' -Context 0,20
  ```

### Troubleshooting

| What you see | Cause | Fix |
| --- | --- | --- |
| `Could not load file or assembly '...'` in the log or console | A file of the release is missing (single files were copied) | Deploy the whole release folder again with `Deploy-ErfWeb.cmd` |
| Service will not start (`Cannot start service ERFWeb.Api`) | A missing file, a bad `appsettings.json`, or the port is taken | Run `C:\inetpub\wwwroot\erf_web-app\ERFWeb.Api.exe` in a console to see the error; `netstat -ano \| findstr ":5080 :5081"` shows what holds the port |
| `/erf/health` shows `"database":"schema_outdated"` (503) | The backend needs a `Database/` script that has not been run (`requiredSchemaVersion` says which) | Run the missing script(s) (step B), then deploy again |
| Sign-in answers "Too many failed sign-in attempts" (423) | Five wrong passwords in a row for that username | Wait 15 minutes, or an administrator resets the password (that also unlocks it) |
| "Too many requests" (429) for many users at once | `ReverseProxy:KnownProxies` is not set, so all users share one rate limit | Set nginx's address there (or raise `RateLimiting`), then `Restart-Service ERFWeb.Api` |
| Every page says "Please change your password before continuing" (403) | The account's password was set by an administrator | Change it on the Change password page the app opens |
| `/erf/health` shows `"database":"unreachable"` (503) | The API cannot reach PostgreSQL | Check the PostgreSQL service and the connection string in `appsettings.json` |
| `/erf/health` shows the old version | The deploy rolled back, or the release was not deployed | Read the deploy window's last line and the newest log file |
| Sign-in fails with 400 `antiforgery_validation_failed` | Cross-site cookies are off, or the frontend address is not allowed | `CrossSiteCookies: true` and the Render address in `Cors:AllowedOrigins`, then `Restart-Service ERFWeb.Api` |
| Browser console: CORS error | The frontend's address is missing from `Cors:AllowedOrigins` | Add it exactly (`https://…`, no trailing `/`), then restart |
| Sign-in impossible on iPhone, iPad or Mac Safari | Safari blocks cross-site cookies on `onrender.com` | Move the frontend to a `mdmpi.com.ph` address on Render (e.g. `erf.mdmpi.com.ph`), then `CrossSiteCookies: false` |
| `AntiforgeryOptions.Cookie.SecurePolicy = Always, but the current request is not an SSL request` | The API is older than the cross-site cookie fix | Deploy a current release |
| A new feature answers 404 | The frontend went live before the backend that has the endpoint | Deploy the backend (step D) |
| An image or link loads the website page instead | The frontend builds that address without `VITE_API_BASE_URL` | Use `apiUrl(...)` from `shared/api/client.ts` for every API address |
| Email not sent | `Smtp` settings wrong, or the server cannot reach the mail server | `email_deliveries.status` and `smtp_status_code` in the database, and the log file |
| Backup failure email, or "Database backup is MISSING" | `pg_dump` not installed or older than the database server, share not writable, or the task did not run | `D:\ERFWeb-Backups\backup-log.txt`, then Task Scheduler → **ERFWeb Database Backup** → History |
| No SLA reminders | `Reminders:Enabled` is not `true`, SLA days are 0, or nothing is overdue yet | The log line `SLA reminders are off` or `SLA reminder run: …`, and `reminder_deliveries` |

### Local development against the live API

The SPA on your PC can use the deployed API: `.env` / `.env.local` with
`VITE_API_TARGET=https://sr.mdmpi.com.ph/erf` and `VITE_API_PROXY_STRIP_PREFIX=true`, then `npm run dev`. A new
endpoint answers 404 through that proxy until the backend that has it is deployed. A Render rewrite of `/erf/*` to
the API was tried and is not forwarded (Render serves `index.html`), which is why the frontend calls
`https://sr.mdmpi.com.ph/erf` directly.
