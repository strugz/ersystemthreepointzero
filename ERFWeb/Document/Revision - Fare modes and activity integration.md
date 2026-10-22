# Revision: fare modes on fare legs + activity integration

Status: **fare modes (sections 1–5) built 2026-10-22, not yet deployed**; outputs (6) deferred; FWMS (7) waits for IT · Scope: `ERFWeb/` only (desktop app untouched)

## Goal

1. Every fare leg on a Transportation line records **how** the trip was made (Taxi, Grab, Angkas, Jeepney, Pedicab, …), not just From / To / Fare.
2. The fare-mode list is a maintained ERFWeb master-data table, seeded from the legacy `tbFare` list.
3. Expense lines can reference the user's **FWMS field-service transaction** (the activity in the CRM/FWMS project), the same way the desktop "FWMS Transaction Lookup" does. Then a claim is tied to the service visit that caused it (see §7).

## What the legacy system does (reference only)

| Legacy object | Behaviour |
| --- | --- |
| `dbo.tbFare (id, SortID, FareName varchar(25))` | The list of fare types. No seed rows are in the repo; the values live in the SQL Server database. |
| `dbo.tbExpenseTransportationItem (expense_id, FareID, FareFrom, FareTo)` | **One** fare type + From/To per expense line (`FareID` is `varchar(1)`). No per-leg fare type, no per-leg amount. |
| `sp2_LoadFare @DeptID` | Returns all fares by `SortID`; department `3` does not see fare `id = 4`. |
| `sp2_InsertFare @FareName` (desktop "add" button next to the combo) | Any user can add a fare type; it is inserted just before `SortID = 100` (the pinned last entry, presumably "Others"). |

ERFWeb already stores **multiple legs per line** (`expense_transport_items`: `fare_from`, `fare_to`, `fare_amount`), so the fare mode goes **on each leg**, which is richer than the legacy one-per-line.

## Decisions (taken 2026-10-22 with the proposed defaults)

- [x] **D1 — Seed list.** Seeded with the proposed list below. The live `tbFare` rows were not available, so Admin adds or renames modes in Master data → Fare modes and can record the matching `tbFare.id` in **Legacy tbFare id**.
- [x] **D2 — Required.** Every leg the API saves needs a mode. Legs saved before script 016 stay valid and show "Not specified" until their line is saved again, which then asks for a mode.
- [x] **D3 — Department rule dropped.** No per-department exclusion. Revisit only if someone confirms what legacy fare `id 4` is and why department 3 must not see it.
- [x] **D4 — Admin maintains the list.** Employees pick **Others (specify)** and type the mode; they cannot add list entries (unlike the desktop "add" button).
- [x] **D5 — Receipt flag recorded, not enforced.** `receipt_expected` is stored and shown in Master data only.

Seed (sort order, code, name, receipt usually available):

| # | Code | Name | Receipt |
| --- | --- | --- | --- |
| 10 | `TAXI` | Taxi | yes |
| 20 | `GRAB` | Grab / ride-hailing | yes |
| 30 | `ANGKAS` | Angkas / motorcycle taxi | yes |
| 40 | `JEEP` | Jeepney | no |
| 50 | `TRIKE` | Tricycle | no |
| 60 | `PEDICAB` | Pedicab | no |
| 70 | `BUS` | Bus | yes |
| 80 | `UV` | UV Express / van | no |
| 90 | `RAIL` | MRT / LRT / train | no |
| 100 | `BOAT` | Ferry / boat | yes |
| 110 | `HABAL` | Habal-habal | no |
| 999 | `OTHERS` | Others (specify) — employee types the mode | no |

## To-do list

### 1. Database — `ERFWeb/Database/016_fare_modes.sql` ✅

- [x] `fare_modes` (`code` unique, `name`, `sort_order`, `receipt_expected`, `requires_detail`, `legacy_fare_id`, `is_active`), seeded idempotently.
- [x] `expense_transport_items.fare_mode_id` (nullable FK, `ON DELETE RESTRICT`, indexed) and `fare_mode_detail varchar(60)`.
- [x] Records `schema_versions` 16; commented rollback at the end.
- [x] `SchemaVersion.Required` raised to 16, so `/api/health` reports `schema_outdated` until the script is applied.
- [ ] **Apply `016_fare_modes.sql` to `erms_db` by hand before deploying the API.**

### 2–4. Backend ✅

- [x] Domain `FareLegRules` (pure, unit-tested): mode required per leg; unknown or inactive modes rejected unless the line already used that mode; typed mode required for "Others", max 60; negative fares rejected. Errors are keyed `transports[i].fareModeId` / `transports[i].fareModeDetail`.
- [x] `ExpenseTransportItemDto` gains `FareModeId`, `FareModeDetail` (read on save) and `FareModeCode`, `FareModeName` (filled on the way out).
- [x] `ExpenseLineService` validates the legs and stores mode and detail. `ExpenseReportDetailLoader` returns code and name on both load paths.
- [x] `FareModeAdminService` + `IFareModeAdminService` (own contract file, so it doesn't touch the shared master-data contract): list / create / update / deactivate, audited as `Admin.MasterDataChanged`. A duplicate code answers 400 with a `code` field error, like the other master data.
- [x] `GET /api/lookups/fare-modes`; `GET|POST /api/admin/master-data/fare-modes`, `PUT …/fare-modes/{id}`.
- [x] Not done: regenerating `0001_initial_schema.sql`. It is a generated snapshot, not kept in step with scripts 002–015 either.

### 5. Frontend ✅

- [x] Types, `expensesApi.fareModes()`, loaded with the other editor lookups and passed to the line dialog.
- [x] Fare-leg row: **Mode** select with an icon per mode, before From → To → Fare. A "Specify the mode" field appears for Others. On a phone, the mode sits on its own row.
- [x] "Add leg" starts from the previous leg's **To** and copies its mode.
- [x] A leg without a mode is flagged on Save (not before), and the server's per-leg errors show on the right field. A mode deactivated after a line was saved still shows on that line.
- [x] Admin → Master data → **Fare modes** tab (code, name, sort order, legacy id, "employee types the mode", receipt flag, active).
- [x] Read-only views: no read-only screen lists fare legs today (the lines table and approval views show line totals only), so nothing else needed a mode column.

### 6. Outputs — deferred

- [ ] R47 PDF: the R47 does not print fare legs today (Transportation is one amount per line in the TRANSP. column). Adding legs changes the fixed form layout, so confirm with Finance first.
- [ ] Summaries / exports carry no line detail, so a fare-mode column needs a new line-level export. Confirm whether Finance wants it.

### 7. FWMS transaction lookup (activity integration)

#### 7a. Legacy reference (desktop, read-only — do not modify)

FWMS is **not an HTTP API**. The desktop app reads the CRM SQL Server database directly with Entity Framework 6, and ERFWeb will do the same with its own read-only EF Core context (see 7c).

| Piece | Legacy file |
| --- | --- |
| Dialog "FWMS Transaction Lookup" (Date From / Date To / Search, grid, Insert / Cancel, double-click inserts) | `ER System/Presentation/Forms/Expense/frmFwmsTransactionLookup.vb` |
| Button on the expense form + `ApplyFwmsTransaction` | `ER System/Presentation/Forms/Expense/frmEReport.vb:719-732` |
| Service (resolves the signed-in user, guards `dateTo < dateFrom`) | `ERSystem.AppServices/Services/Crm/TransactionLookupService.vb` |
| Query | `ERSystem.Infrastructure/Data/Repositories/TransactionRepository.vb` |
| DbContext (connection string name `FwmsDb`, database `CRM_DB`, SQL Server 2012 instance) | `ERSystem.Infrastructure/Data/FwmsDbContext.vb`, `ER System/app.config` |
| Table models | `ERSystem.Domain/Entities/Crm/{TradeMasterModel,ContactMasterModel,AccountMasterModel}.vb` |
| Result DTO | `ERSystem.Domain/Dtos/Crm/FwmsTransactionDto.vb` |

The date-range query, as written in LINQ:

```text
TRDMST trade
  JOIN CNTMST  contact ON trade.TRDMUI = contact.CNTMID     -- technician who owns the transaction
  JOIN ACCMST_ account ON trade.TRDMAC = account.ACCMID     -- hospital / customer account
WHERE trade.TRDMCD >= @from AND trade.TRDMCD < @to + 1 day  -- transaction (created) date
  AND contact.CNTMNN = @userInitial                         -- user's legacy username (registry "username")
  AND trade.TRDMTT LIKE '%FLD%'                             -- field-service SR numbers only
ORDER BY trade.TRDMCD DESC, account.ACCMNM, trade.TRDMTT
```

There is also a lookup by one exact SR number (`GetFWMSBySRNumber`, the same joins plus `TRDMTT = @sr`).

What gets copied into the expense line. **ERFWeb already has all four columns on `expense_lines`**, so this mapping needs no schema change:

| FWMS column | Shown as | Legacy control | ERFWeb `ExpenseLine` |
| --- | --- | --- | --- |
| `ACCMST_.ACCMNM` | Hospital Name | `txtLocation` | `Location` |
| `TRDMST.TRDMDE` | Instrument Model | `txtInstrument` | `Instrument` |
| `TRDMST.TRDMTT` | S.R. No | `txtServiceNumber` | `ServiceNumber` |
| `TRDMST.TRDMMC` | Serial Number | `txtSerialNumber` | `SerialNumber` |

Selected but not used by the desktop app: `ACCMSC` (account short code), `CNTMNN`, `TRDMTY` (type), `TRDSEC`, `TRDSTS` (status), `TRDMCD` (date). Not selected but possibly useful for fare legs: `TRDLOC` / `TRDLOCOUT`, `TRDITI` / `TRDITISVC`, `TRDADT`, `TRDARE`. Their meaning has to be confirmed (decision F4).

#### 7b. Decisions

- [x] **F1 — Access path: ERFWeb reads `CRM_DB` directly through its own read-only EF Core context** (decided 2026-10-22, replacing the HTTP-endpoint idea). This mirrors the desktop `FwmsDbContext`, and no FWMS-project work is needed.
- [ ] **F2 — Network (prerequisite, currently blocked).** The ERFWeb server can't reach `CRM_DB` today. IT must allow the ERFWeb server → CRM SQL Server (`CRM_DB` on the `sql2012` named instance). That means TCP to the instance's port (fix it to a static port, or also open UDP 1434 for SQL Browser), firewalled to the ERFWeb server's IP only. Until this is open, the feature ships disabled (503).
- [ ] **F2b — Login.** IT/DBA creates a **dedicated** SQL login for ERFWeb with `SELECT` on `dbo.TRDMST`, `dbo.CNTMST` and `dbo.ACCMST_` only. Don't reuse the desktop's `FwmsDb` login, because its rights are unknown and it is shared by every desktop install.
- [ ] **F2c — TLS.** Check whether the SQL 2012 instance has the TLS 1.2 update. If it does, use `Encrypt=True;TrustServerCertificate=True`, or a trusted certificate. If it doesn't, the connection needs `Encrypt=False` on the LAN. That is a documented risk: credentials and results are then unencrypted between the two servers.
- [ ] **F3 — User matching.** Legacy matches `CNTMST.CNTMNN` to the desktop registry username. Is the ERFWeb `users.username` the same value? Recommended: add `users.fwms_contact_code` (nullable, set by Admin, falls back to `username`) so a mismatch never shows someone else's transactions.
- [ ] **F4 — Fare legs from FWMS.** Do `TRDLOC` / `TRDLOCOUT` (or `TRDITI*`) hold the visit's from/to locations? If yes, map them too, and "Insert" can pre-fill a fare leg (BASE → hospital). A read-only `SELECT TOP 20` on a known transaction will answer this.
- [ ] **F5 — Link and double-claim.** Store the FWMS transaction id (`TRDMST._id`) on the line? Recommended: yes, for traceability. Warn (not block) when the same SR number is already on another active line of the same user.

#### 7c. ERFWeb backend: the `CRM_DB` EF Core context

Layering follows the existing ERFWeb rules: the contract lives in Application, and everything SQL-Server-specific stays in Infrastructure.

**Package and context**

- [ ] Add `Microsoft.EntityFrameworkCore.SqlServer` `10.0.*` to `ERFWeb.Infrastructure.csproj`, next to Npgsql. This is the only new package. `TreatWarningsAsErrors` stays on, so check the restore for vulnerability warnings.
- [ ] `Infrastructure/Fwms/FwmsDbContext.cs`, a second context, completely separate from `ErfDbContext`:
  - [ ] `UseSqlServer(conn, o => o.UseCompatibilityLevel(110).CommandTimeout(options.CommandTimeoutSeconds))`. Level 110 = SQL 2012, so EF does not emit `OPENJSON` for collection parameters.
  - [ ] `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`.
  - [ ] Override `SaveChanges` / `SaveChangesAsync` to throw `InvalidOperationException("CRM_DB is read-only")`, as a guard on top of the read-only login.
  - [ ] **No migrations and no snake_case naming convention**. CRM column names stay exact, and `EFCore.NamingConventions` must not apply to this context.
  - [ ] Make sure `tools/GenerateSchemaScript.cs` and any `dotnet ef` design-time factory target only `ErfDbContext`. With two contexts, `dotnet ef` needs `--context`, so document that in the README.

**Entities** (`Infrastructure/Fwms/Entities/`): hand-written, mapping only the columns used. Don't scaffold all of `CRM_DB`.

- [ ] `FwmsTrade` → `dbo.TRDMST`: `ID` (key, int), `_id` → `ExternalId`, `TRDMAC`, `TRDMUI`, `TRDMTT`, `TRDMDE`, `TRDMMC`, `TRDMTY`, `TRDSTS`, `TRDMCD` (datetime?), plus `TRDLOC` / `TRDLOCOUT` if F4 confirms them.
- [ ] `FwmsContact` → `dbo.CNTMST`: `ID`, `CNTMID`, `CNTMNN`.
- [ ] `FwmsAccount` → `dbo.ACCMST_` (note the trailing underscore): `ID`, `ACCMID`, `ACCMSC`, `ACCMNM`.
- [ ] Map column names explicitly with `HasColumnName`, and string columns as `varchar` (`IsUnicode(false)`) so the parameters match the column type. Otherwise SQL Server converts the column and can't use its indexes. Confirm the lengths with a read-only `INFORMATION_SCHEMA.COLUMNS` query before coding.
- [ ] Joins are on string codes (`TRDMUI = CNTMID`, `TRDMAC = ACCMID`), not foreign keys. Write them as explicit LINQ `join`s, as the legacy repository does, rather than as navigation properties.

**Options and registration**

- [ ] `FwmsOptions`, bound from the `Fwms` section of `appsettings.json`: `ConnectionString`, `CommandTimeoutSeconds` (default 10) and `MaxRangeDays` (default 31). Add the keys without values to `appsettings.Example.json`. The real connection string exists only in the server's `appsettings.json`, and is never logged or committed.
- [ ] In `DependencyInjection.cs`, register `FwmsDbContext` **only when `ConnectionString` is set**. Otherwise register an `UnavailableFwmsTransactionLookup` that makes the endpoints answer **503**, as `Lasius` / `Smtp` do. The API must start and `/api/health` must stay healthy even when CRM is down or unconfigured.

**Lookup service**

- [ ] Application contract `IFwmsTransactionLookup` (`Application/Features/Fwms/`), with `SearchAsync(DateOnly from, DateOnly to, CancellationToken)` and `GetBySrNumberAsync(string sr, CancellationToken)`. It returns `FwmsTransactionDto(TransactionId, TransactionDate, SrNumber, HospitalName, AccountCode, InstrumentModel, SerialNumber, Status)`.
- [ ] Infrastructure `FwmsTransactionLookup` is a faithful port of `TransactionRepository.vb`:
  - [ ] the same three joins
  - [ ] `TRDMTT.Contains("FLD")`
  - [ ] `TRDMCD >= from && TRDMCD < to.AddDays(1)`
  - [ ] ordered by `TRDMCD desc, ACCMNM, TRDMTT`
  - [ ] the by-SR variant uses the same joins plus `TRDMTT == sr`, **and** is scoped to the current user's contact. The legacy version isn't scoped this way, but the web version must be.
- [ ] The contact code comes from the **signed-in user** (F3), never from the request.
- [ ] Validate `to >= from` and the range ≤ `MaxRangeDays` (400). Trim the input, and reject an SR number longer than the column.
- [ ] Error mapping: a `SqlException` or timeout → `503` ProblemDetails ("FWMS is unavailable, try again"). Login failure (error 18456) is logged as an error so Admin sees it, without the connection string.
- [ ] Logging: only the duration and row count. Never log hospital names, SR numbers or serials.

**Endpoints and storage**

- [ ] `GET /api/lookups/fwms-transactions?from=&to=` and `GET /api/lookups/fwms-transactions/by-sr/{srNumber}` in `LookupsController`, for signed-in users. The controller only maps.
- [ ] If F5 or F3: script `017_fwms_link.sql` (PostgreSQL, `erms_db`) adds `expense_lines.fwms_transaction_id varchar(50) NULL` + index and/or `users.fwms_contact_code varchar(50) NULL`, ending with the `schema_versions` row. This script is for ERFWeb's own database only; **nothing is ever written to `CRM_DB`**.

**Tests**

- [ ] Query semantics run against an in-memory provider or a fake `IQueryable` source: date bounds (an exclusive end date), the `FLD` filter, user scoping (including by-SR), and ordering.
- [ ] `SaveChanges` throws.
- [ ] 503 when unconfigured, and when a `SqlException` occurs.
- [ ] 400 on a bad range.
- [ ] The architecture test still passes: Application has no reference to the SqlServer provider.
- [ ] Manual check against the real `CRM_DB` (read-only) once F2 is open: same date range and user in the desktop lookup and the web lookup, same rows.

#### 7d. Frontend: the web "FWMS lookup"

The desktop dialog in the screenshot is the functional reference. The web version keeps the same flow and fixes its rough edges.

- [ ] **Entry point:** a "Find in FWMS" text button beside **Location** in `ExpenseLineDialog.vue` (Location is the first field it fills). Also show it inside **More details** next to Instrument / S.R. no. / Serial.
- [ ] **Lookup dialog** (`features/expenses/components/FwmsLookupDialog.vue`), opened over the line dialog:
  - [ ] Date From / Date To default to the **line's date**, not today (legacy defaults to today, so users re-pick the date every time). It searches on open and whenever a date changes, so no Search button is needed.
  - [ ] Results as rows: hospital (primary text), instrument · serial (secondary), S.R. no. as a monospace chip, date on the right. At phone width the rows stack as cards. Don't use a 4-column grid that scrolls sideways.
  - [ ] A quick filter box above the list (hospital, S.R. no., serial) filters client-side.
  - [ ] One click or tap on a row inserts it. Keyboard: ↑/↓ moves the selection and Enter inserts, so the legacy double-click is not needed. Esc closes.
  - [ ] States: loading (3 skeleton rows, no spinner flash under ~150 ms), empty ("No FWMS service reports for you between {from} and {to}."), 503 ("FWMS lookup isn't set up on this server." — the fields stay typeable by hand), and error with Retry.
- [ ] **Insert behaviour:** fill Location, Instrument, S.R. no. and Serial. Auto-expand **More details** so the user sees what changed. If any of those fields already held a different value, show a snackbar "Filled from FWMS {SR}" with **Undo**, which restores the previous values, instead of a confirm dialog first.
- [ ] Show a small "FWMS · {SR}" link badge on the line (and in approval and Finance views) when `fwms_transaction_id` is set (F5).
- [ ] **Motion** (kept small because this runs many times a day):
  - [ ] The dialog uses the app's existing Vuetify dialog transition. There is no extra animation on result rows: no stagger and no row entrance, because a list shown dozens of times a day should feel instant.
  - [ ] Row press feedback: `transform: scale(0.98)` on `:active`, `transition: transform 120ms ease-out`. Hover background only under `@media (hover: hover) and (pointer: fine)`.
  - [ ] The filled fields get a one-time background tint that fades out (`background-color` 600 ms `ease-out`). It shows the user where the values landed, which is a state indication, not decoration. Under `prefers-reduced-motion` the tint is static for 1 s, then removed.
  - [ ] Keyboard-driven selection changes (↑/↓) move the highlight instantly, with no transition.
  - [ ] Transitions name exact properties, never `transition: all`.

### 8. Verification

- [ ] Backend tests: fare-mode validation (missing, inactive, Others without detail, excluded department), lookup ordering, admin CRUD + duplicate code 409, detail loader projection.
- [ ] From `ERFWeb/Backend`: `dotnet build ERFWeb.sln`, `dotnet test ERFWeb.sln`, `dotnet format ERFWeb.sln --verify-no-changes`.
- [ ] From `ERFWeb/Frontend/erfweb-client`: `npm run lint`, `npm run build`.
- [ ] Manual: add/edit a Transportation line with 2+ legs of different modes; reopen an old line with legs saved before 016 (shows "Not specified", saving forces a choice per D2); PDF and summary output.

### 9. Release order

1. Apply `016_fare_modes.sql` to `erms_db` (by hand).
2. Publish and deploy the backend (`Publish-ErfWeb.cmd` → `Deploy-ErfWeb.cmd`); health check must pass.
3. Merge to `master` so Render rebuilds the SPA.
4. FWMS lookup ships as its own release once F2 is open: IT opens the firewall and creates the read-only login, put `Fwms:ConnectionString` in the server's `appsettings.json`, apply 017 (if F5/F3), deploy the ERFWeb backend, then the SPA. Without the connection string the release is safe: the lookup answers 503.
