# ER System 3.0 User Manual

## Purpose

This manual explains the main ER System 3.0 workflows by role. ER System is a Windows desktop application used to create expense reports, manage approvals, review finance receipt status, and maintain user account settings.

Role access depends on the logged-in user's account level and authority setup. Some menus may be hidden if the account is not assigned to that role or workflow.

## Common Navigation

### Login

1. Open ER System.
2. Enter Username and Password.
3. Click Login.

If connection settings have not been configured for the Windows profile, the Connection form opens before login. Contact an administrator or deployment owner for approved database connection details.

### Main Window

The main window shows the user's ERF list and role-based menus.

Common menus and controls:

- Account Settings
- Forms
- Command
- Help
- Search and Reset
- Report details grid
- File Report
- Print Preview
- Report Data

Keyboard commands:

- `Ctrl + L`: logout.
- `Esc`: close or clear the active workflow, depending on the current screen.

### Help

Open Help from the Help menu when basic application guidance is needed. Contact support when help content does not match the current workflow.

## User Guide

The User role is for employees who create, update, file, and track their own expense reports.

### Update My Account Settings

1. Open Account Settings.
2. Select My Account Settings.
3. Review user details, department, email, rates, approvers, and signature.
4. Update allowed fields.
5. Click Save.

Important notes:

- User ID and username are read-only in My Account Settings.
- Signature is used for approval/report workflows.
- Meal and transportation rates affect expense calculations.
- Missing rates can prevent expense entry until an administrator updates the account.

### Change Password or Email Setup

1. Open Account Settings.
2. Select Change Password/Email Setup.
3. Update password or email settings.
4. Save changes.

Use the approved company email settings. Incorrect email setup can affect report notification behavior.

### Create a New ERF

1. On the main window, click Report Data.
2. Select the ERF report type:
   - Replenishment of Revolving fund
   - Liquidation for Cash Advance
   - Reimbursement
3. Enter the report date range.
4. Enter the purpose of expense.
5. Review or enter the ERF reference number.
6. Enter cash advance or revolving fund details when required by the selected report type.
7. Attach scanned receipts if available.
8. Click Save.

Report type behavior:

- Replenishment requires revolving fund information.
- Liquidation requires cash advance amount information.
- Reimbursement is used when the user is claiming expenses without a cash advance liquidation requirement.

### Attach Scanned Receipts

1. In the report details form, click Browse for attachments.
2. Select one or more PDF, JPG, JPEG, or PNG receipt files.
3. Confirm the selected files appear in the attachment field.
4. Save the ERF.

Keep physical receipts even when scanned receipts are attached. Finance may still require physical receipt submission.

### Edit an Existing ERF

1. Select an ERF from the main report grid.
2. Right-click and select Edit Report Details, or open the ERF from the grid.
3. Update the allowed report details.
4. Click Update.

If an ERF has already moved through approval or finance review, some changes may require approval or support coordination.

### Add or Update Expenses

1. Open an ERF from the main report grid.
2. Go to the expense entry area.
3. Select or enter expense category, particulars, amount, multiplier, invoice, status, remarks, location, service number, instrument, serial number, and work-with information as required.
4. Click Save to add a new expense line.
5. Double-click an expense line to edit it.
6. Click Update to save changes to an existing line.

For meal, transportation, allowance, and per diem entries, the system may calculate or restrict amounts based on the user's configured rates.

### File a Report

1. Complete report details and expense lines.
2. Confirm required scanned receipts are attached, when applicable.
3. Select the report in the main grid.
4. Click File Report.
5. Confirm the report is ready for approval.

After filing, the report moves into the approval workflow.

### Print or Preview a Report

1. Select the report from the main grid.
2. Click Print Preview.
3. Review the report output.
4. Print only when the report details are correct.

Report viewing and printing require the Crystal Reports runtime on the workstation.

### View Previous ERs

1. Open Account Settings.
2. Select Previous ER.
3. Search or select the ERF to review.

Use Previous ER to check older reports, references, and status history.

### Physical Receipt Reminder

When a User logs in, ER System may show a reminder if Finance is waiting for physical receipts for approved ERFs.

If this appears:

1. Review the listed or counted missing receipt items.
2. Submit the physical receipts to Finance.
3. Ask Finance to mark the receipts as received.

## Admin Guide

The Admin role manages account setup, user access, departments, signatories, connection setup, and workflow configuration. Some admin functions may also be available only to users with specific authority rows.

### Configure Database Connection

1. Open the Connection form when prompted, or use the configured admin path.
2. Select Microsoft SQL Server as the data source.
3. Enter server name.
4. Choose Windows Authentication or SQL Authentication.
5. Enter database name.
6. Click Test.
7. Click Save after the test succeeds.

Connection settings are saved under the current Windows user's ER System registry settings. Do not share database passwords through screenshots, manuals, or release notes.

### Create a User Account

1. Open Account Settings.
2. Select User Account.
3. Enter User ID, full name, position, department, username, password, email recipients, user level, approvers, and rates.
4. Add a signature image when required.
5. Click Save.

User levels used by the current application include:

- Admin
- User
- Finance

### Update a User Account

1. Open User Account.
2. Double-click the user row.
3. Update allowed fields.
4. Click Update.

Admin should verify the following after changes:

- Department is correct.
- User level is correct.
- Approver 1 and Approver 2 are correct.
- Meal and transportation rates are complete.
- Signature is present when required.
- Email To and Email BCC are correct.

### Manage Signatories

1. Open Account Settings.
2. Select Signatory.
3. Add or update department signatory setup.
4. Save changes.

Users may be blocked from report work if required signatory or approver setup is missing.

### Manage My Account Settings for a User

Use My Account Settings for the logged-in account when a user needs to update profile, approver, rate, signature, or authority information through the newer account settings form.

Admin should be careful when changing:

- User Level
- Department
- Approver 1
- Approver 2
- User Authority rows
- Employee Rates

These fields affect menu access, approval routing, and expense calculations.

### Approval Queue

1. Open Forms.
2. Select For Approval.
3. Review pending reports.
4. Open report details before approving or rejecting.
5. Approve, reject, or return according to company policy.

Approval actions should be performed only after reviewing report details, scanned receipts, and expense lines.

### Previous Forms

1. Open Forms.
2. Select Previous Forms.
3. Search or select the needed report.
4. Review the report status and details.

Use Previous Forms to support follow-up questions and audit checks.

## Finance Guide

The Finance role reviews approved ERFs, tracks physical receipts, adds finance remarks, and sends SMS notifications.

### Open Finance Review

1. Log in with a Finance user account.
2. Open Forms.
3. Select Finance Review.

The Finance Review menu appears only when the logged-in account has the Finance user level.

### Filter the Finance Queue

Finance Review includes filters for:

- Employee
- Status
- Receipts
- ERF Type
- Date filter
- From date
- To date

Use Refresh after changing filters.

Common filter values:

- Status: Pending, Receipts Received, All
- Receipts: Missing, Received, All
- ERF Type: All, Replenishment of Revolving fund, Liquidation for Cash Advance, Reimbursement

### Review ERF Details

1. Select an ERF row in the Finance Review grid.
2. Review the ERF Details panel.
3. Confirm employee, description, type, date range, ERF reference number, cash reference number, cash amount, revolving fund, finance status, physical receipt status, received date, scanned receipt deletion date, and remarks.

### Mark Physical Receipts Received

1. Select the ERF row.
2. Enter Finance Remarks if needed.
3. Click Receipts Received.
4. Confirm the success message.
5. Refresh the queue or verify the row status updated.

The Receipts Received button is disabled when the selected ERF already has physical receipts marked as received.

### Send SMS Notification

1. In Finance Review, click Send in the SMS column for the target ERF.
2. Review the recipients list.
3. Select a notification template:
   - Physical receipts reminder
   - Receipts received notice
   - Finance follow-up
4. Review the message preview.
5. Click Send.

If no recipients are found, the Send button is disabled. Ask Admin to verify the user's account, contact, and authority setup.

### Missing Physical Receipts Process

Use this process for approved ERFs with missing physical receipts:

1. Filter Receipts to Missing.
2. Select the ERF.
3. Review ERF details and reference number.
4. Send a Physical receipts reminder SMS when appropriate.
5. Wait for physical receipts from the user.
6. Enter remarks and click Receipts Received after Finance receives the documents.

### Finance Troubleshooting

- If Finance Review does not open, verify the user level is Finance.
- If the queue fails to load, confirm the finance tracking database script was applied.
- If SMS recipients are missing, verify user account contact and authority setup.
- If the Receipts Received button is disabled, check whether receipts were already marked received.
- If report details are incomplete, ask the user or admin to verify the ERF and account setup.

## Support Notes

Common issues and likely causes:

- Login fails: incorrect username/password, missing database connection, or inactive account.
- Main menu options are missing: user level or authority setup does not allow that workflow.
- Expense amount is wrong: employee rates may be missing or incorrect.
- Report printing fails: Crystal Reports runtime may be missing or mismatched.
- Finance Review fails to load: database migration may be missing.
- Scanned receipt save fails: database attachment table or permissions may be missing.
- Connection works for one Windows user but not another: registry settings are per Windows profile.

When escalating to support, include:

- Windows username.
- ER System username.
- User level.
- Workstation name.
- Error message.
- Report ID or ERF reference number.
- Steps performed before the issue occurred.
