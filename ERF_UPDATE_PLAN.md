# ERF Update Plan

## Objective
Prepare an implementation roadmap for the requested ERF enhancements in the legacy VB.NET WinForms application while keeping changes incremental, low-risk, and compatible with the current .NET Framework 4.8 solution structure.

## Requested Enhancements
1. Add a reference number for every ERF.
2. Add an ERF type dropdown with these options:
   - Replenishment of Revolving Fund
   - Liquidation for Cash Advance
   - Reimbursement
3. Add scanned receipt attachment support.
4. Prompt users when receipts are incomplete.
5. Add a finance module for viewing and completing ERFs.
6. Delete scanned receipts after an ERF is approved.

## Current-State Findings

### Main workflow touchpoints
- `ER System/Presentation/Forms/Expense/FrmEReportDetails.vb`
  - Current ERF header creation and update flow.
  - Already contains `txtRefNum` and `txtRefDoc`, plus cash advance and revolving fund fields.
  - Uses legacy `AddReport(...)` and `UpdateReport(...)` functions from `modMaintenance.vb`.
- `ER System/Presentation/Forms/Expense/frmEReport.vb`
  - Main expense detail entry form.
  - Handles report detail loading through temporary text files under `Application.StartupPath`.
  - No current receipt attachment workflow found.
- `ER System/Presentation/Forms/Expense/frmERType.vb`
  - Used for sending/export-related behavior, not as the ERF category/type selector requested here.
- `ER System/Presentation/Forms/Approval/frmApprove.vb`
  - Current approval workflow for viewing user reports, opening reports, and approving/rejecting them.
  - Likely closest existing entry point for a future finance review/completion workflow.
- `ER System/Legacy/Modules/modMaintenance.vb`
  - Contains legacy report and expense save/update logic.
  - Uses concatenated SQL/stored procedure calls and is a high-risk but central integration seam.
- `ER System/Infrastructure/Data/Legacy/ClsLoadData.vb`
  - Uses file-based temporary state and reporting helpers.
  - No receipt metadata storage or cleanup behavior exists today.

### Database findings from the current schema
- `tbReportDetails` is the current ERF header table.
  - Existing fields include `ID`, `ReportDateFrom`, `ReportDateTo`, `ReportDescription`, `UserID`, `ReportStatus`, `ReportEndorseStatus`, `ReportDateFiled`, `ReportFileStatus`, `ReportPrintStatus`, `ReportReturnedForModi`, `ReportNumberStatus`, `ReportCancelNote`, `ReportAttachment`, and `ReportSentStatus`.
  - `ReportAttachment` is a single nullable `varchar(255)` field, so the current database only supports one report-level attachment path/value.
  - `ReportNumberStatus` already exists, but it is used by the approval progression workflow rather than as a business ERF reference number.
- `tbCashAdvance` stores the current cash-advance-related header details for each report.
  - Existing fields include `CashAmount`, `CashDate`, `CashRefDoc`, `CashRefNo`, `BalanceTo`, `RevolvingFund`, and `CashCheck` keyed by `ReportID`.
  - `CashRefNo` already exists and appears to store the current cash advance reference number, not a dedicated ERF-wide reference number.
- `tbExpenseDetails`, `tbExpenseMealItem`, and `tbExpenseTransportationItem` store expense-line data and sub-items.
  - No receipt attachment or receipt completeness fields exist in these tables.
- `tbReportAuthority` stores approval signatures per report.
- `tbUserRegistration` contains approval configuration fields including `Approver1`, `Approver2`, `ReportNumberStatus`, and `SuperApprover`.

### Database findings from current stored procedures
- `sp2_AddReportData`
  - Inserts the ERF header into `tbReportDetails` and inserts cash advance metadata into `tbCashAdvance`.
  - Does not accept or store ERF type, finance workflow state, multiple attachments, or receipt completeness metadata.
  - Generates `tbReportDetails.ID` using `NEWID()`.
- `sp2_UpdateReportData`
  - Updates only `tbReportDetails` date/description fields and `tbCashAdvance` values.
  - Does not update attachment fields, ERF type, or finance-specific fields.
- `sp2_InsertAttachment`
  - Updates only `tbReportDetails.ReportAttachment`.
  - Current attachment persistence is single-value and report-level only.
- `sp2_UpdateReportNumberStatus`, `sp2_UpdateReportNumberStatus_ByPass`, `sp2_UpdateReportStatus`, and `sp2_RefileER`
  - Drive the approval/filed/done workflow using `ReportNumberStatus`, `ReportFileStatus`, `ReportPrintStatus`, `ReportReserveStatus1`, `ReportReserveStatus2`, and `ReportEndorseStatus`.
  - No finance completion state or receipt cleanup action exists in the database workflow.

### Important constraints
- The project is a legacy VB.NET WinForms application targeting .NET Framework 4.8.
- Repository guidance requires incremental restructuring and avoiding big-bang changes.
- Designer files should be protected unless UI layout changes are required.
- Database changes are high-risk and must be isolated and documented.

## Recommended Delivery Strategy
Implement the requested features in phases so the application remains buildable and testable after each step.

## Phase 1 - Define ERF header data and database changes
### Goals
- Make ERF reference number mandatory and unique.
- Add ERF type as a structured field.
- Prepare storage for receipt metadata and finance completion state.

### Proposed work
- Base the header design on the existing split between `tbReportDetails` and `tbCashAdvance`.
- Add a dedicated ERF reference number field to `tbReportDetails` instead of reusing `tbCashAdvance.CashRefNo`.
  - Recommended new column: `ErfReferenceNumber varchar(50)`.
  - Recommended constraint: unique index once backfill and format rules are defined.
- Add a dedicated ERF type field to `tbReportDetails`.
  - Recommended new column: `ErfType varchar(50)`.
- Add finance workflow fields to `tbReportDetails` rather than overloading the approval counters.
  - Recommended new columns: `FinanceStatus varchar(20)`, `FinanceCompletedBy int`, `FinanceCompletedDate datetime`, `FinanceRemarks varchar(255)`.
- Do not rely on `ReportNumberStatus` for the new ERF reference number because it is already part of the approval routing logic.
- Do not rely on `ReportAttachment` for the new receipt feature beyond short-term compatibility because it only supports one nullable string value.
- Add a dedicated receipt table for normalized attachment metadata.
  - Recommended new table example: `tbReportReceiptAttachment` with fields such as `ID`, `ReportID`, `ExpenseID` nullable, `StoredFileName`, `StoredPath`, `OriginalFileName`, `ContentType`, `UploadedBy`, `UploadedDate`, `IsComplete`, `DeletedDate`, `DeletedBy`, and `IsDeleted`.
- Update these existing procedures or add versioned replacements:
  - `sp2_AddReportData`
  - `sp2_UpdateReportData`
  - report list/load procedures that need ERF type or finance status in the UI
  - approval/finalization procedures if receipt cleanup or finance transition is database-driven
- Decide how reference numbers are generated:
  - preferred: SQL Server-generated business reference using a controlled sequence/table logic separate from `tbReportDetails.ID`
  - fallback: application-generated formatted value only if database-managed generation is not feasible

### Deliverables
- SQL script or documented schema/stored procedure update plan grounded in `tbReportDetails`, `tbCashAdvance`, and the approval procedures
- field mapping document for report header, cash advance data, receipt metadata, and finance state

## Phase 2 - Update ERF entry UI and save/update logic
### Goals
- Capture the new ERF type and required reference number in the ERF creation/edit flow.
- Keep changes centered on the existing report header form.

### Proposed work
- Update `ER System/Presentation/Forms/Expense/FrmEReportDetails.vb` and its designer to:
	- add a dedicated ERF reference number control instead of reusing the current cash advance reference fields
  - add a dropdown for ERF type with the exact options:
	- Replenishment of Revolving Fund
	- Liquidation for Cash Advance
	- Reimbursement
- Add validation rules such as:
  - reference number required
  - ERF type required
  - cash-advance-related fields only required when the selected ERF type needs them
- Keep `txtRefDoc` and `txtRefNum` mapped to `tbCashAdvance.CashRefDoc` and `tbCashAdvance.CashRefNo` only when cash advance information is relevant
- Extend the current `AddReport(...)` and `UpdateReport(...)` flow to persist the new ERF header fields alongside the existing `tbCashAdvance` values
- Prefer introducing a new report service/repository wrapper rather than expanding form code directly

### Suggested extraction seam
Create a small feature-focused service and repository pair, for example:
- `ER System/Application/Services/ErfHeaderService.vb`
- `ER System/Infrastructure/Data/Repositories/ErfHeaderRepository.vb`

Keep the existing module calls as compatibility seams only if full replacement is not yet practical.

## Phase 3 - Add scanned receipt attachment support
### Goals
- Let users attach scanned receipts to an ERF.
- Store enough metadata for finance review and later cleanup.

### Proposed work
- Extend the expense/ERF flow with an attachment action, likely from:
  - `FrmEReportDetails` for report-level attachments, or
  - `frmEReport` if attachments must be tied to detailed expense lines
- Keep the existing `tbReportDetails.ReportAttachment` and `sp2_InsertAttachment` only as legacy compatibility paths if current screens still depend on them.
- Implement the new receipt feature with normalized metadata instead of storing all receipt information in `ReportAttachment`.
- Decide storage approach:
  - preferred: file storage in a controlled application folder with metadata in a new attachment table
  - alternate: database blob storage only if infrastructure or audit requirements demand it
- Add receipt metadata model/service classes under the new architecture folders, for example:
  - `ER System/Domain/Entities/ReceiptAttachment.vb`
  - `ER System/Application/Services/ReceiptAttachmentService.vb`
  - `ER System/Infrastructure/Data/Repositories/ReceiptAttachmentRepository.vb`
- Add validations for file type, file size, missing file, and duplicate attachment scenarios

### Storage recommendation
Prefer filesystem storage plus SQL metadata because the current application already uses filesystem-based export/temp patterns, while the current database only has a single-path `ReportAttachment` field and no receipt blob design.

## Phase 4 - Add incomplete receipt prompting
### Goals
- Warn users before filing/submitting an ERF when expected receipts are missing or incomplete.

### Proposed work
- Define what “incomplete receipts” means in business terms. Recommended first version:
	- expenses exist but no receipt attachments exist in the new receipt table for receipt-required categories
  - required metadata for uploaded receipts is missing
  - manually flagged incomplete receipts by the user or finance reviewer
- Add a validation step before filing/submitting the ERF.
- Surface the prompt in the filing action instead of passive display only.
- Return a clear message such as:
  - which expenses are missing receipts
  - whether user can continue with warning or must stop

### Suggested implementation seam
- Add a validator under:
  - `ER System/Application/Validation/ReceiptCompletenessValidator.vb`
- Call it from the form before the report is filed/sent for approval.

## Phase 5 - Add finance module for viewing and completion of ERF
### Goals
- Give finance users a dedicated place to review ERFs and mark them complete.

### Proposed work
- Review whether `frmApprove` should be extended or a separate finance form should be added.
- Preferred low-risk direction:
  - add a new finance-focused form instead of overloading the approval screen immediately
- Use a dedicated finance status instead of reusing `ReportFileStatus`, `ReportPrintStatus`, or `ReportNumberStatus`, because those fields already drive the current approval routing and done/filed state.
- Candidate additions:
  - `ER System/Presentation/Forms/Finance/frmFinanceErfQueue.vb`
  - finance service/repository classes under `Application` and `Infrastructure`
- Finance module features should include:
  - list/filter ERFs pending finance action
  - open ERF header/details
  - view attached scanned receipts
  - mark ERF as completed
  - record finance remarks or exceptions

### Dependency
This phase depends on the schema updates from Phase 1 and receipt metadata support from Phase 3.

## Phase 6 - Delete scanned receipts after approval
### Goals
- Remove stored scanned receipt files after the ERF reaches the approved state, while keeping needed audit metadata.

### Proposed work
- Define the exact trigger:
  - immediate deletion after final approval
  - or deletion after finance completion if finance is the final business checkpoint
- Add a cleanup routine that:
  - verifies the ERF status is final approved
  - deletes files from storage
	- updates receipt metadata as deleted with timestamp/user/system marker in the new receipt table
  - handles missing-file cases safely without crashing the approval flow
- If the trigger remains final approval, integrate cleanup with the workflow currently driven by `sp2_UpdateReportNumberStatus` and `sp2_UpdateReportNumberStatus_ByPass`, but keep file deletion orchestration in application/service code rather than directly in forms

### Suggested implementation seam
- `ER System/Application/Services/ReceiptCleanupService.vb`
- call it from the final approval or finance completion workflow service

## Cross-Cutting Refactoring Recommendations
To keep the code maintainable, use each requested feature as a reason to continue the existing incremental architecture improvement:
- keep new SQL and workflow logic out of forms
- avoid adding more public global state to legacy modules
- use parameterized SQL in any new repository code
- introduce feature-specific services instead of extending `modMaintenance.vb` indefinitely
- keep WinForms designer edits limited to required controls only

## Proposed Implementation Order
1. Confirm database/stored procedure impact for report header, receipt metadata, and finance state.
2. Add ERF reference number and ERF type support in the report header form.
3. Refactor save/update of ERF header into a dedicated service/repository seam.
4. Add receipt attachment upload and metadata persistence.
5. Add receipt completeness validation before filing/submission.
6. Add finance queue/form for viewing and completing ERFs.
7. Add receipt deletion/cleanup after final approval or finance completion.
8. Build and smoke-test after each phase.

## Key Risks
- Stored procedures currently rely on string-concatenated SQL execution in legacy modules.
- Database changes may affect approval/reporting screens and Crystal Reports, especially `sp_rptER`, report list procedures, and any screen that expects the old `tbReportDetails` shape.
- Attachment storage and deletion need clear audit requirements before implementation.
- A finance module may require role/security updates beyond UI changes.
- If report exports or historical reporting need receipt retention, immediate deletion may conflict with audit needs.
- `ReportNumberStatus` already has existing meaning in approval routing, so reusing it for a visible ERF reference number would create behavioral risk.
- `ReportAttachment` is currently single-value only, so expanding receipt support without a new table would be brittle.

## Open Questions To Resolve Before Implementation
1. Should the new ERF reference number be user-entered, auto-generated, or both?
2. Should ERF type live only at the report header level in `tbReportDetails`, or is there any need to tie it to expense lines?
3. Should scanned receipts be attached per report, per expense item, or support both with `ExpenseID` optional in the new receipt table?
4. Which expense categories require mandatory receipts?
5. Should users be blocked from filing when receipts are incomplete, or only warned?
6. Is finance completion a separate step after the current approval workflow reaches `DONE`, or should finance become part of the existing approval progression?
7. After approval, should scanned receipts be physically deleted immediately, archived, or retained temporarily for audit?

## Suggested First Increment
For the safest first implementation, start with:
1. ERF reference number
2. ERF type dropdown
3. validation updates in `FrmEReportDetails`

This delivers visible business value with the smallest footprint before adding attachment lifecycle and finance workflow complexity.
