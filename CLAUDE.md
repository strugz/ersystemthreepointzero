@AGENTS.md

<!--
Maintainer note: AGENTS.md is the single source of truth for repo-wide agent rules
and is shared with Codex and Copilot. Do not copy its content here — the import
above loads it in full. Add only Claude-Code-specific facts below.
Nested guides load automatically: Web/CLAUDE.md, Web/Backend/CLAUDE.md, and
Web/Frontend/CLAUDE.md each import their sibling AGENTS.md on demand.
-->

## Claude Code

### Shell and scripts

PowerShell is the primary shell on this machine. Repository scripts under `tools/` are run as:

```
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\<script>.ps1
```

`tools/Invoke-WebCleanArchitectureAudit.ps1` (report-only) and `tools/Verify-ScannedReceipts.ps1` (read-only) are safe to run. `tools/Register-WebCleanArchitectureAudit.ps1` writes to Windows Task Scheduler and must only be run when explicitly requested.

### Build and test entry points

Use the verification skills rather than assembling these commands by hand:

- `/verify-desktop` — restore, build `ER System.sln`, run the MSTest suite
- `/verify-web` — restore, build, test, and format-check `Web/Backend/ERSystem.Web.sln`
- `/verify-frontend` — `npm ci`, lint, test, build the Vue client

### Toolchain facts that are easy to get wrong

- **Desktop tests are MSTest on .NET Framework 4.8.** Run them with `vstest.console.exe`. `dotnet test` does not work against `ERSystem.Tests`.
- **Desktop projects use `packages.config`, not SDK-style PackageReference.** Restore with `nuget restore "ER System.sln"`; `dotnet restore` is not a substitute.
- **`ERSystem3.5Setup/ERSystem3.5Setup.vdproj` cannot be built by MSBuild.** It requires `devenv.com`. Exclude it from solution builds and treat installer validation as a separate, explicit step.
- **The web backend sets `TreatWarningsAsErrors=true`** (`Web/Backend/Directory.Build.props`). Any new compiler warning fails the build.
- **Resolve Visual Studio paths at runtime with `vswhere`**, never hardcode them:
  ```
  & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
  ```
  Verified-good local versions: .NET SDK 10.0.400, Node 24.18.0 / npm 11.16.0, Visual Studio 18. Pin machine-specific absolute paths in `CLAUDE.local.md`, not here.

### Rules most often violated silently

These are already in `AGENTS.md` but bear repeating because the failure is quiet:

- No new business logic or SQL in WinForms code-behind, Vue components, or API controllers.
- Never execute `ER3.0.sql` wholesale and never auto-apply EF Core migrations. The web application does not own the legacy schema; database changes go in a new dated script under `Database/`.
- Do not revert or clean up unrelated user changes in the working tree.

### Clean up superseded code

When an approach is revised mid-task (a first attempt did not match the requirement and a new function, component, prop, endpoint or test replaced it), remove what the new version made obsolete in the same change. Before reporting done:

- Review the full diff (`git diff`), not just the last edit, and check every function, variable, prop, emit, import, type, CSS class, test and doc line this task added is still used. Search for callers before keeping or deleting anything.
- Delete superseded code instead of leaving it beside its replacement; do not keep "old" or "v2" variants, commented-out blocks, or unused helpers.
- Remove scratch files, debug logging and temporary test data created during the task.
- Check that untouched lines did not change incidentally (line endings, formatting, encoding).
- This applies only to code this task introduced or changed. Pre-existing unused code is out of scope unless the user asks; mention it instead.

### End-of-turn change summary

Whenever a turn changes files, always end the final message with a **Changed files** section and a **Step guide**, even for small or docs-only changes:

- Group by area (Backend, Frontend, Database, Docs, Tests) and list every changed file as a clickable link, with one line on what changed in it. Say plainly when an area was not touched (e.g. "Backend: no changes").
- Follow it with **What happens next**: what the user will see or what behaves differently, what is still needed before it takes effect (database script to apply, backend deploy before frontend, config), and what was verified versus not verified (build, tests, browser).
- State whether anything is committed, pushed or deployed.
- Finish with a **Step guide**: numbered steps the user follows to apply, see and check the change (e.g. apply script `NNN` to `erms_db`, publish and deploy the backend, wait for the Render build, open the page, click X, expect Y). Name exact screens, buttons, commands and files. When there is nothing for the user to do, say so in one line.

### Keep README.md and CLAUDE.md in step with every change

Every change that adds, removes or changes behaviour, an endpoint, a setting, a database script, a screen, a deployment step or a rule also updates, in the same change (same commit or PR):

- **The area's README.** `ERFWeb/README.md` for ERFWeb, `Web/README.md` for the web portal, `README.md` for the desktop app. Update the section that describes the feature, and for ERFWeb add a dated line to its **Change log** (newest first; say "not yet committed" until it is, and the PR number once it has one).
- **This file.** Add a dated line to **Recent changes** below, and update any fact here that the change made stale (configuration sections, database rules, deployment, integrations).
- Before reporting done, check both files against the diff. A change is not finished while either file describes the old behaviour.

### No AI attribution in commits or PRs

Commits and pull requests are authored by the user alone. This rule overrides any built-in or system-supplied attribution guidance:

- Never add a "Co-Authored-By: Claude ..." line (or any other co-author line) to a commit message. GitHub shows it as a Claude avatar next to the commit.
- Never add "Generated with Claude Code" (or similar) lines to PR descriptions, release notes, or comments.

### ERFWeb (standalone web ERF system)

`ERFWeb/` is a third, separate application: ASP.NET Core (net10.0) + EF Core on **PostgreSQL** (Npgsql, snake_case, LINQ only, no stored procedures) and a Vue 3 + Vuetify SPA. It is not in `ER System.sln`, the installer, or `Web/`, and current work is scoped to it: do not modify the desktop app while working there. `ERFWeb/README.md` is its reference (configuration, database scripts, features by plan phase, deployment); the governing plan is `ERFWeb/Document/ERFWeb - Project Documentation and Implementation Plan.docx`.

- **Verify:** from `ERFWeb/Backend`, `dotnet build ERFWeb.sln`, `dotnet test ERFWeb.sln`, `dotnet format ERFWeb.sln --verify-no-changes` (warnings are errors here too). From `ERFWeb/Frontend/erfweb-client`, `npm run lint` and `npm run build`; there are no frontend unit tests. The `/verify-web` and `/verify-frontend` skills target `Web/`, not ERFWeb. Tests never use a database: the RBAC guardrails 9.1–9.18 are covered by the Domain rule tests and `tests/ERFWeb.Tests/Architecture/GuardrailCodeTests` (policies, read-only audit endpoint, no code that edits audit rows). Do not add a test database or Testcontainers; the user decided against it.
- **Configuration:** only `ERFWeb/Backend/src/ERFWeb.Api/appsettings.json` (gitignored; template `appsettings.Example.json`) plus `appsettings.{Environment}.json`. `Program.cs` clears every other source, so environment variables and user secrets are ignored. `Lasius` (SMS), `Smtp` (email) and `Crm` (CRM employee lookup) are optional sections; the API starts without them and those features answer 503. `Reminders` (`Enabled`, `RunAt`) is optional too and off unless `Enabled` is `true`, so only the server sends SLA reminders. `App:Mode` (`Test` or `Production`, default `Production`) gates the test-only deletes; the SPA's matching build variable is `VITE_APP_MODE` (`test` or `prod`; the committed `.env` keeps `prod` because Render builds from it). Never print or commit its contents.
- **Database:** numbered forward scripts `ERFWeb/Database/NNN_*.sql` (idempotent, commented rollback at the end), applied by the user by hand to `erms_db`. Add a new numbered script for schema changes; never run `dotnet ef database update` against it. Read-only checks against it are fine. Since script `021`, `workflow_audit` is append-only in the database (a trigger refuses UPDATE, DELETE and TRUNCATE); a maintenance script that must delete audit rows opts in with `SET LOCAL erfweb.allow_audit_purge = 'on'`. Application code does so only in `Infrastructure/Testing/TestDataService` (the test-only report and audit-entry deletes, `App:Mode` `Test`, System Administrator), which the guardrail test exempts by file name; nothing else may.
- **Deployment:** follow the "Deployment guide" in `ERFWeb/README.md`. Backend: `Backend/tools/Publish-ErfWeb.cmd` builds `ERFWeb/releases/ERFWeb-Api-<version>-<commit>/`, and `Deploy-ErfWeb.cmd` in that folder installs it on the server (Windows Service `ERFWeb.Api` in `C:\inetpub\wwwroot\erf_web-app`, behind nginx at `https://sr.mdmpi.com.ph/erf/`, which maps `/erf/*` to `/api/*`) with backup, health check and automatic rollback. Never copy single DLLs: a missing dependency fails only the endpoints that load it. Releases never contain `appsettings.json`; the server keeps its own. Frontend: Render static site `https://ersystemthreepointzero.onrender.com`, built from `master`; deploy the backend first. The local SPA `.env` proxies to the deployed API, so new endpoints 404 locally until deployed.
- **Versions (bump on every change merged to `master`):** the API version is `<Version>` in `ERFWeb/Backend/Directory.Build.props` (`/erf/health` shows it as `<version>+<commit>`); the SPA version is `"version"` in `Frontend/erfweb-client/package.json` and `package-lock.json` (set both with `npm version <level> --no-git-tag-version`; the app shows `ERFWeb v<version>`). Every PR to `master` that changes an app bumps that app in the same PR: backend changes bump the API, frontend changes bump the SPA, a change to both bumps both, each at its own level. Decide the level from what the change does and name it (major, minor or patch) in the PR description and the change-log line:
  - **Major** (`x.0.0`): breaks compatibility. An endpoint, request or response field removed or changed so the other side's deployed version no longer works with it; a database script the previous release cannot run against; a feature removed or a workflow rule changed so users must work differently.
  - **Minor** (`0.x.0`): new and backward compatible. A new screen, feature, setting, endpoint, optional field or additive database script that older clients and the previous release tolerate.
  - **Patch** (`0.0.x`): no new behaviour. A bug fix, wording, style, performance or refactor change.
  - A PR that changes only docs, tests or developer tooling bumps nothing and says "no version change". The release folder name (`ERFWeb-Api-<version>-<commit>`) and the health check then tell each deployed build apart.
- **Local API runs:** the local `appsettings.json` binds Kestrel to the server's LAN address, so run the built DLL with a scratch content root and an `appsettings.<Env>.json` Kestrel override (`.claude/launch.json` has `erfweb-client-local-api` for the SPA side, port 5179 → localhost:5081).
- **Notifications:** `IApprovalNotifier` is layered Logging → SMS (Lasius) → Email (MailKit, R47 attached). Delivery runs after the workflow commits and never fails it; each send is claimed once per idempotency key in `sms_deliveries` / `email_deliveries`, and only counts (never numbers, addresses or text) go to logs and `workflow_audit`. Verification steps set `WorkflowEventNotification.DeliveryKey` and go only to their `RecipientUserIds` (`ISmsService.SendToUserAboutReportAsync` per person, `IReportEmailService.SendToUsersAboutReportAsync` once), not to the claimant and signatories. Daily SLA reminders (Phase 8.4) are separate: `ApprovalReminderJob` (a `BackgroundService` in the API) runs `ApprovalReminderService` once a working day; it uses the SMS and SMTP gateways directly (there is no signed-in user) and claims each reminder once in `reminder_deliveries`. The job records each run in `workflow_audit` (`ReminderRun` with counts, or `ReminderRunFailed`), which the Rules page's SLA reminders card reads. Timing rules are in Domain `ApprovalReminderRules` and the Settings SLA days.
- **Backups and monitoring (Phase 8.6):** not part of the API. Two Windows scheduled tasks on the API server, set up with `Backend/tools/Install-ErfWebMaintenance.cmd` (settings at its top) and documented step by step in `Backend/tools/README.txt`: a nightly `pg_dump` (`Backup-ErfWebDatabase.ps1`) and a 5-minute check of `/erf/health` and the backup's age (`Test-ErfWebHealth.ps1`), both reading the DB login and `Smtp` from the server's `appsettings.json`. PostgreSQL 18 runs on a different server from the API; the API server uses pgAdmin's `runtime` folder for `pg_dump`/`pg_restore`. These are Windows PowerShell 5.1 scripts: keep them ASCII-only and `.cmd` files CRLF.
- **CRM:** an account links to the CRM by the CRM `employeeId` in `users.crm_employee_id` (the CRM has no employee email); CRM activity calls take that ID. An expense line pre-filled from CRM activities stores their `activityId`s in `expense_lines.crm_activity_ids` (several per line when one visit served several instruments; their service no., instrument and serial are joined with " / "; one activity: many lines of one report, never a second non-Rejected report; `CrmActivityClaimCheck`). The old `crm_activity_id` column is emptied by script `023` and no longer read. Only the ERFWeb server calls the CRM (`CrmEmployeeDirectory`, `CrmActivityDirectory`, shared `CrmApi`, `X-Api-Key`), logging only HTTP status, never search text, IDs or the key. The contract is `ERFWeb/Document/CRM-API-Integration-Spec.md` plus the CRM team's "CRM Employee and Activity API for ERFWeb" doc; follow their field names exactly (e.g. `mobileNumber`).
- **Cost center budgets:** a cost center is one department's (one active per department) with one overall budget per calendar year (`cost_center_budgets`). Monitoring only: never block filing or approval on it and never print it on the R47. Spending counts against `expense_reports.department_id`, set at filing. Finance sees every cost center; a signatory those of the departments their Department or Exclusion scope covers today (`CostCenterBudgetService`).
- **R47 Work With:** group headers print `<location> Work w/: <initials>` (Domain `R47Layout.WorkWithInitials`); full Work With names never print on the R47. Line order on the R47 is the saved `expense_lines.sort_order`, set by the editor's Sort menu through `PUT /api/reports/{id}/lines/order`.
- **PDF libraries:** two, both only in `ERFWeb.Infrastructure` (`Infrastructure/Reporting`); never remove QuestPDF. The **R47** engine is part of the R47 format (below): `QuestPdf` (default, `R47Document`) or `PdfSharp` (`R47PdfSharpDocument`, fonts from the Windows fonts folder via `PdfSharpFonts`). Neither lays the R47 out: `R47Composer` places every box, rule, text and image once from the R47 design (grid text wrapped by Domain `R47TextMetrics.Wrap` with the design's `R47GridMetrics`), and both documents only draw those shapes, so they print the same form. An R47 change goes into `R47Composer` (and, if the designer should control it, Domain `R47Design` / `R47DesignRules` plus the SPA's `features/admin/r47Design.ts`), never into one engine; `R47PdfRendererTests` runs on both engines. **Exports** (`ExportPdfDocument`) and any other new layout: QuestPDF. **Existing PDFs** (merge, split, import pages, overlay, page count): PDFsharp. **PDFsharp is pinned to exactly 6.2.4**, the last MIT release; 6.5.0+ is AGPL-3.0 or a paid licence and refuses to run without `PdfSharpLicense` in code, so never upgrade without a licence decision. Log page counts only, never uploaded PDF content or names. Details: "PDF libraries" in `ERFWeb/README.md`.
- **R47 format:** set on the Rules page (**Expense report (R47) → Format of the R47**): PDF engine plus header details (company name and short name, title, document code, effectivity date, revision no.), Domain `R47Format` / `R47FormatRules` (required, length limits that fit the header boxes). Stored as `Reports.R47Engine` … `Reports.R47RevisionNo` in `app_settings`, audited `SettingsChanged`, read at print time by `ExpenseReportDocumentLoader` (`ISystemSettingsService.GetR47FormatAsync`) and applied by `R47PdfRenderer` through `R47ReportOptions.WithFormat`; no restart. A key never saved falls back to the server's `R47Report` value in `appsettings.json`. Font families and the UTC offset stay in `appsettings.json` only.
- **R47 designer:** **Rules → Expense report (R47) → Open the R47 designer** (`/admin/r47-designer`, System Administrator) sets the R47 layout: margins, band heights, the 13 grid column widths (they fill the width between the margins), header, cash advance and footer widths, type sizes, two colours and the fixed texts (the promise aligned Left, the standard, or Center). Domain `R47Design` (standard values = the Crystal form) and `R47DesignRules` (ranges, form fits the page, promise fits the footer). Band order and content are the controlled form and do not change; the grid font stays Tahoma (page breaks measure Tahoma widths). Stored as JSON in one `app_settings` row `Reports.R47Design`, audited `SettingsChanged` with the changed values only, read at print time by `ExpenseReportDocumentLoader`; none saved prints the standard form with `R47Report:DetailHeight`. Page breaks follow the design (`R47Layout.BuildRows` with `R47GridMetrics`, `Paginate` with its detail height). `POST /api/admin/r47-design/preview` prints the made-up `R47SampleReport` or a report picked through `GET /api/admin/r47-design/reports` (drafts included, since a report gets its ER reference only at filing; only reports the caller may view) with an unsaved layout and either engine; it saves nothing.
- **R47 signatures:** the setting `Reports.R47SignatureMode` (Rules page) is `ESignature` (default: approved images print with the ELECTRONIC SIGNATURE watermark and "Electronically signed in ERFWeb <date time> · <ref>") or `StatusOnly` (no images; "Electronically approved/filed <date time>"); Domain `R47Signatures`. An image prints only after Finance approves it (`users.signature_approved_at`, script `024`, **Signatures** page, `SignatureApprovalService`); a new upload resets the approval, and nobody reviews their own (Domain `SignatureApprovalRules`). Never print a signature image without the watermark.
- **Finance rules:** the Finance requirements of 2026-11-11 are built as rules on the Rules page (**Finance rules** card), one phase per PR (plan: weekly cutoff, daily meal report, claim eligibility, Sponsorship/Demo documents, Lodging, Gas/Medical verification, Work With meals, gas allowance and coding days). Stored as one JSON row `Business.FinanceRules` in `app_settings`, Domain `FinanceRuleSet` (every property has a default, so JSON saved by an earlier phase still loads; add new rules as properties with defaults) validated by `FinanceRuleSetRules`, read through `ISystemSettingsService.GetFinanceRulesAsync` at save or file time. Employees see a rule's effect through `my-filing-readiness` / `my-rates`, never the admin endpoint. Phase 1, the weekly processing cutoff (`FilingCutoffRules`, default Wednesday 12:00), is a notice only and never blocks filing; with Flag for Finance it sets `expense_reports.filed_late`.

### Recent changes

Newest first; the full entries are in the **Change log** of `ERFWeb/README.md`.

- **2026-11-11:** Expense line **Work with** picks several people (chips), saved as one comma-separated text as before; frontend only, SPA 0.6.0 (PR #52).
- **2026-11-11:** Finance rules phase 2, daily meal claims report: Finance **Meal claims** page (`/summaries/meals`; `GET /api/summaries/meals` and `/meals/export`, Finance policy) lists per day who claimed which meals, by date range and department, with a CSV. No database script. API 0.4.0, SPA 0.6.0 (minor); deploy backend before frontend (PR #52).
- **2026-11-11:** Finance rules phase 1, weekly processing cutoff: **Rules → Finance rules** (`Business.FinanceRules`); the filing checklist and File dialog say which Friday the report is processed in; Flag for Finance marks late reports **Late** in the Finance queue (script `025`, `SchemaVersion.Required` 25). API 0.3.0, SPA 0.5.0 (minor); apply `025`, deploy backend before frontend (PR #51).

- **2026-11-10:** Rule **No AI attribution in commits or PRs**: no "Co-Authored-By" lines in commits and no "Generated with Claude Code" lines in PRs, release notes or comments. Docs only (PR #50).
- **2026-11-10:** Expense line dialog shows instrument, serial no., service no. and remarks open under **Service details** (no More details toggle); frontend only, SPA 0.4.1 (PR #49).
- **2026-11-10:** Version bump rule: every PR to `master` bumps the API (`Directory.Build.props`) and/or the SPA (`package.json`) it changes, as major, minor or patch (see **Versions** above). API 0.1.1 → 0.2.0 and SPA 0.3.1 → 0.4.0 (minor) for the sign-in lockout settings (PR #48).
- **2026-11-10:** Sign-in lockout set on the Rules page (**Sign-in security**: 3 to 20 failed attempts, default 5; lock 1 to 1440 minutes, default 15), stored as `Security.LoginMaxFailedAttempts` / `Security.LoginLockoutMinutes` in `app_settings`, Domain `LoginLockoutPolicy`; applies to the next lock; no database script; deploy backend before frontend (PR #47).
- **2026-11-10:** ERFWeb feature overview deck: `ERFWeb/Document/ERFWeb Feature Overview.html` (21 slides, one self-contained HTML file in the frontend-slides style; arrow keys to present, E to edit) and its PDF. Docs only (PR #49).
- **2026-11-05:** Documents cleaned up: the five ERFWeb role guides updated and exported to `ERFWeb/Document` (Word and PDF), the Implementation Plan brought up to date, the finished fare-modes revision note removed; desktop-era root files (`ERF_UPDATE_PLAN.md`, `ER3.0 *.txt` schema dumps, `Hospital List.xlsx`, `Updated features on ER System.docx`, the intro decks in `outputs/`) removed. `ER3.0.sql` stays as the schema reference (PR #45).
- **2026-11-05:** R47 designer on the Rules page (`/admin/r47-designer`): drag band edges and column dividers or set exact sizes, type, colours and fixed texts, preview on a real report with either engine, save as `Reports.R47Design` in `app_settings` (`/api/admin/r47-design`). Both engines now draw the shapes `R47Composer` lays out from Domain `R47Design`; page breaks follow the design; the standard layout prints as before. No database script; deploy backend before frontend (PR #44).
- **2026-11-05:** The R47 can be drawn with PDFsharp (new `R47PdfSharpDocument`); QuestPDF stays the default and draws the exports. PDFsharp pinned to 6.2.4 (last MIT release). The System Administrator sets the R47 format on the Rules page (PDF engine and header details, `Reports.R47*` rows in `app_settings`, falling back to `R47Report` in `appsettings.json`); no database script; deploy backend before frontend (PR #44).
- **2026-11-03:** R47 signature rule on the Rules page (E-signature with an ELECTRONIC SIGNATURE watermark, or Status only) and Finance approval of each signature image on a new **Signatures** page (`/api/signatures`, audited `Admin.SignatureApproved` / `Admin.SignatureRejected`); script `024`, `SchemaVersion.Required` 24 (PR #43).
- **2026-11-03:** `Database/maintenance/reset_reports.sql` (delete every report and its audit trail) also deletes the report-related `TestData.Deleted` and earlier `Reports.ResetByScript` audit rows, which carry no `report_id` (PR #42).
- **2026-11-03:** Test mode: `App:Mode` `Test` (API) plus `VITE_APP_MODE=test` (SPA) give the System Administrator a Delete button on every record a create or update makes (`DELETE /api/test-data/{kind}/{id}`); a report is hard-deleted with everything under it, its `workflow_audit` rows included, the Audit log can delete single entries, and the user page role grants (own account: revoked only); audited `TestData.Deleted`; no database script (PR #42).
- **2026-10-29:** ERMS renamed to **ERS, Expense Report System** on the sign-in page and Healthy living page (logo component `AppErsMark`); frontend only. `erms_db` and the Lasius `Application` value `ERMS` are unchanged (PR #41).
- **2026-10-29:** The quote-of-the-day Bible verses use modern English (World English Bible, British Edition, public domain) instead of the KJV; frontend only (PR #40).
- **2026-10-29:** The System Administrator can remove a user's signature in the user console (`DELETE /api/admin/users/{id}/signature`, audited `Admin.SignatureDeleted`); earlier R47s print without it too, since signatures are read at print time (PR #39).
- **2026-10-29:** The employee's signature is optional for filing (the printed R47 goes to Finance and can be signed there): no longer a filing blocker in Domain `ReportWorkflowRules.FilingBlockers`; optional grey item in the **Before you file** checklist (PR #39).

- **2026-10-28:** The quote of the day alternates daily between a healthy-living saying and a Bible verse (KJV, shown with its reference; `bibleVerses` in `features/wellness/quotes.ts`); frontend only (PR #38).
- **2026-10-28:** A **Quote of the day** dialog on the first signed-in page each day (once per user per browser, `DailyQuoteDialog`); the Healthy living page moved to `/random-quotes` (old `/healthy-living` redirects); frontend only (PR #37).
- **2026-10-28:** public **Healthy living** page (`/healthy-living`, no sign-in; Wellness nav group and a link on the sign-in page): a quote of the day from `features/wellness/quotes.ts` plus healthy-habit tips; frontend only (PR #36).
- **2026-10-27:** Each SLA reminder run is recorded in `workflow_audit` (`ReminderRun` / `ReminderRunFailed`) and shown on an **SLA reminders** card on the Rules page (`GET /api/admin/reminders/status`) (PR #35).
- **2026-10-27:** R47 ENDORSED BY and APPROVED BY signatures print above the names, not over them (PR #34).
- **2026-10-27:** **Approve** on the R47 PDF page when opened from a review and the viewer may approve (PR #33).
- **2026-10-27:** One expense line can link several CRM activities (checkboxes in the From CRM picker; service no., instrument and serial joined with " / "; script `023`, `SchemaVersion.Required` 23) (PR #32).
- **2026-10-27:** Scope assignments can be edited in full (signatory, level, type, department, effective-from), not only ended and re-created (PR #32).
- **2026-10-26:** Verification steps send SMS and email to the claimant, Expense Verifiers, Finance and Level 2 as each step concerns them (PR #32). Cost center budgets: one cost center per department with a yearly budget, a **Budgets** page for signatories and Finance, filing department saved on the report (script `022`, `SchemaVersion.Required` 22; PR #31). R47 Work With prints as initials (PR #30). Sort expense lines into the R47 order (PR #29). CRM picker previous/next day arrows and File report / Re-file report from the R47 preview (PR #28). Phase 8.4 SLA reminders, 8.5 guardrail tests and append-only `workflow_audit` (script `021`), 8.6 nightly backup and health monitor (PR #27).

### Other agent configuration

`.github/copilot-instructions.md` and `.codex/` exist for Copilot and Codex. Their guidance is already covered by `AGENTS.md`; nothing needs to be mirrored here.
