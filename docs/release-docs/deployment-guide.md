# ER System 3.0 Deployment Guide

## Purpose

This guide describes how to prepare, deploy, verify, and roll back an ER System 3.0 release. ER System 3.0 is a legacy VB.NET Windows Forms application targeting .NET Framework 4.8 and SQL Server-backed data stores.

Use this guide for production releases, UAT refreshes, and controlled workstation installs.

## Release Scope

The current release package should include:

- The ER System Windows Forms application built from `ER System.sln`.
- The installer project output from `ERSystem3.5Setup` or the installer project selected for the release.
- Database scripts from `Database`.
- Updated release documentation from `docs/release-docs`.

Important application areas in this release:

- Login and connection setup.
- Expense report creation and editing.
- Report type capture:
  - Replenishment of Revolving fund
  - Liquidation for Cash Advance
  - Reimbursement
- ERF reference number capture.
- Scanned receipt attachment storage.
- Approval and previous form review.
- Finance ERF review.
- Physical receipt tracking.
- SMS reminders and finance notices.
- Account settings, user account setup, signatory setup, and email setup.

## Prerequisites

### Workstation Requirements

- Windows workstation supported by the organization.
- .NET Framework 4.8 installed.
- Network access to the ER System SQL Server.
- Network access to the CRM/FWMS SQL Server if FWMS transaction lookup is used.
- Crystal Reports runtime compatible with CrystalDecisions 13.0.4000.0, x64.
- Permission to write current-user registry settings under `HKEY_CURRENT_USER\Software\ER System`.

### Build Machine Requirements

- Visual Studio with VB.NET desktop workload.
- .NET Framework 4.8 targeting pack.
- Visual Studio setup project support if building `.vdproj` installers.
- NuGet packages restored from `packages`.
- Crystal Reports developer/runtime components compatible with the project references.
- Access to all referenced project dependencies.

### Database Requirements

- SQL Server database used by ER System.
- SQL Server database used by CRM/FWMS integration, when enabled.
- Permission to run schema migration scripts.
- Full database backup before migration.

Do not store production credentials in release documentation. Confirm credentials through the approved deployment channel.

## Pre-Deployment Checklist

- Confirm the release source branch and commit.
- Confirm all intended code changes are included.
- Confirm no unrelated local changes are mixed into the release build.
- Back up the ER System database.
- Confirm the target database name and server.
- Confirm the CRM/FWMS connection target, if used.
- Confirm Crystal Reports runtime availability on client workstations.
- Confirm the installer project to build.
- Confirm deployment window and affected users.
- Confirm rollback owner and rollback window.
- Notify users of expected downtime or restart requirements.

## Database Migration

Run database scripts against the ER System database before deploying the client application.

Apply scripts in this order:

1. `Database/20260521_AddReportTypeToReportDetails.sql`
2. `Database/20260522_CreateReportFinanceTracking.sql`
3. `Database/20260602_AddErfReferenceNoToReportDetails.sql`
4. `Database/20260602_AlterReportAttachmentToVarcharMax.sql`
5. `Database/20260602_CreateScannedReceiptAttachment.sql`

### Migration Verification

After the scripts finish, verify:

- `tbReportDetails.ReportType` exists.
- `tbReportDetails.ERFReferenceNo` exists.
- `tbReportDetails.ReportAttachment` supports `varchar(max)`.
- `tbReportFinanceTracking` exists.
- `tbReportFinanceTracking.ReportID` has a unique constraint.
- `tbScannedReceiptAttachment` exists.
- `tbScannedReceiptAttachment.ReportID` has an index.
- Existing ERF records are still visible in the application.

If any script fails, stop deployment and restore from the database backup or correct the database issue before continuing.

## Build Procedure

1. Open `ER System.sln` in Visual Studio.
2. Restore NuGet packages if Visual Studio does not restore them automatically.
3. Select the release configuration used by the team.
4. Confirm the platform target is x64 for the main application.
5. Build the full solution.
6. Build the setup project selected for the release, such as `ERSystem3.5Setup`.
7. Collect the installer output from the setup project output folder.
8. Record the build date, source branch, commit, installer file name, and database script set.

## Client Deployment Procedure

1. Close ER System on the target workstation.
2. Confirm .NET Framework 4.8 is installed.
3. Confirm the Crystal Reports runtime is installed.
4. Install the new ER System package.
5. Start ER System.
6. If the connection setup form appears, enter the approved SQL Server connection details and click Test.
7. Save the connection when the test succeeds.
8. Log in with a test or authorized production account.
9. Confirm the user sees the expected menus for their role.

Connection settings are stored per Windows user under the current-user ER System registry keys. A workstation used by multiple Windows users may need connection setup for each Windows profile.

## Post-Deployment Smoke Test

Perform a short verification after deployment:

- Login succeeds.
- Main report list loads.
- Search and reset work on the main report list.
- New ERF opens from the report details button.
- Report type list shows all expected report types.
- ERF reference number is populated or can be entered.
- Scanned receipt attachment selection accepts PDF and image files.
- Existing ERF opens from the main grid.
- Expense details can be viewed for an ERF.
- For approver/admin users, For Approval opens.
- Previous Forms opens for review history.
- For Finance users, Finance Review appears under Forms.
- Finance Review loads the queue after the finance tracking script is applied.
- Finance can mark physical receipts as received.
- Finance SMS dialog opens from the SMS button.
- Change Password/Email Setup opens.
- My Account Settings opens for the logged-in user.
- Ctrl + L logs out.
- Escape closes the expected active form or clears the current workflow.

## Rollback Procedure

Use rollback only when a release creates a production issue that cannot be corrected safely during the deployment window.

1. Stop using ER System on affected workstations.
2. Reinstall the previous ER System client package.
3. Restore the ER System database from the pre-deployment backup if database changes must be reversed.
4. Reapply only the database scripts required by the restored client version.
5. Confirm login and existing ERF workflows.
6. Record the rollback reason, affected users, database backup used, and final application version.

Database rollback should be coordinated carefully because users may have created reports, attachments, approvals, or finance receipt entries after deployment.

## Known Deployment Risks

- Crystal Reports runtime mismatch can prevent report viewing or printing.
- Missing database scripts can prevent Finance Review and receipt attachment features from loading.
- Per-user registry connection settings can make one Windows profile work while another fails on the same workstation.
- SQL Server permissions can allow login but fail later during report, attachment, or finance actions.
- Network access to the CRM/FWMS database affects transaction lookup behavior.

## Release Record Template

Use this template for each deployment:

```text
Release name:
Deployment date:
Deployment owner:
Source branch:
Source commit:
Installer project:
Installer file:
Database backup:
Database scripts applied:
Target database:
Target workstations/users:
Smoke test result:
Issues found:
Rollback required:
Notes:
```
