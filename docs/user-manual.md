# ER System 3.0 User Manual

## Purpose

ER System 3.0 is a Windows desktop application for preparing, filing, approving, reviewing, and tracking expense reports. This manual is for all application users, including employees, approvers, administrators, and finance users.

Access to screens and actions depends on the logged-in account's user level, department, and authority setup. If a menu or button described here is not visible, contact an administrator to verify your account setup.

## User Roles

| Role | Main Responsibilities |
| --- | --- |
| User | Create ERFs, add expenses, attach scanned receipts, file reports, print reports, and track previous ERs. |
| Approver or Signatory | Review submitted reports, approve valid reports, reject or return reports that need correction, and view previous forms. |
| Admin | Maintain user accounts, departments, signatories, account settings, rates, email setup, and connection setup. |
| Finance | Review approved ERFs, track physical receipts, add finance remarks, and send SMS reminders or notices. |

## Getting Started

### Open the Application

1. Start ER System from the installed shortcut or application folder.
2. Wait for the login screen to appear.
3. If the Connection form appears, ask an administrator or deployment owner for the approved database settings.

### Log In

1. Enter your Username.
2. Enter your Password.
3. Click Login.

If login fails, check that your username and password are correct. If the problem continues, contact an administrator to confirm your account is active and properly configured.

### Log Out

Use either option:

- Press `Ctrl + L`.
- Click the logout icon on the main window, if visible.

Logging out clears the active session and returns to the login screen.

### Exit

Click the close or exit control on the main window. Confirm the exit prompt when asked.

## Main Window

The main window displays report data and the menus available to your account.

Common areas:

- Account Settings menu
- Forms menu
- Command menu
- Help menu
- Search panel
- Report grid
- Report Data button
- File Report button
- Print Preview button

Common keyboard commands:

- `Ctrl + L`: log out.
- `Esc`: close or clear the active workflow, depending on the screen.
- `Ctrl + F5`: open connection setup where supported.

## Searching Reports

1. Use the search field on the main window.
2. Select a filter when available.
3. Click Search.
4. Review the matching rows in the report grid.
5. Click Reset to clear the search and grid results.

## User Workflows

### Review My Account Settings

1. Open Account Settings.
2. Select My Account Settings.
3. Review your user details, department, email settings, approvers, rates, authority rows, and signature.
4. Update allowed fields.
5. Click Save.

Important notes:

- User ID and username are read-only.
- Meal and transportation rates affect expense calculations.
- Missing rates can block or affect expense entry.
- Signature information may be required for approval and report output.

### Change Password

1. Open Account Settings.
2. Select Change Password/Email Setup.
3. Open the Change Password tab.
4. Enter the current password.
5. Enter and confirm the new password.
6. Click Change.

If the new password and confirmation do not match, correct the values and try again.

### Update Email Setup

1. Open Account Settings.
2. Select Change Password/Email Setup.
3. Open the Email Setup tab.
4. Review Email Address, Email Password, Email To, and Bcc.
5. Update allowed fields.
6. Click Update.

Use only approved company email settings. Incorrect values may prevent report notifications from sending.

### Create a New ERF

1. Click Report Data on the main window.
2. Select the report type:
   - Replenishment of Revolving fund
   - Liquidation for Cash Advance
   - Reimbursement
3. Enter the report date From and To.
4. Enter the Purpose of Expense.
5. Review or enter the ERF Reference No.
6. Enter cash advance or revolving fund details when required.
7. Attach scanned receipt files when available.
8. Click Save.

Report type rules:

- Replenishment of Revolving fund requires revolving fund information.
- Liquidation for Cash Advance requires cash advance amount information.
- Reimbursement is used for expense reimbursement without cash advance liquidation.

### Attach Scanned Receipts

1. In the report details form, click the browse attachment control.
2. Select one or more receipt files.
3. Supported file types include PDF, JPG, JPEG, and PNG.
4. Confirm the file paths appear in the attachment field.
5. Save or update the ERF.

Keep the original physical receipts. Finance may still require physical receipt submission even when scanned receipts are attached.

### Open an Existing ERF

1. Locate the ERF in the main report grid.
2. Double-click the row to open the report.
3. Review report details and expense lines.

If the report cannot be opened, verify that your account has required rates and signatories configured.

### Edit Report Details

1. Select the ERF in the main report grid.
2. Right-click the row.
3. Select Edit Report Details.
4. Update allowed fields.
5. Click Update.

Reports that have already moved through approval or finance review may require coordination before changes are made.

### Add an Expense Line

1. Open the ERF.
2. Go to the Expense Report area.
3. Enter Date Service.
4. Select Type and Category.
5. Enter Particulars.
6. Enter Amount and Multiplier.
7. Review Total.
8. Enter Invoice No., Status, Remarks, Hospital Name, Service No., Instrument, Serial Number, and Work With when required.
9. Click Save.

The system may calculate amounts automatically for allowance, meal, or transportation entries based on your account rates.

### Update an Expense Line

1. Open the ERF.
2. Double-click the expense row.
3. Update allowed fields.
4. Click Update.

Use Clear or `Esc` to reset the current entry area without saving.

### Add Transportation Details

1. Select a transportation-related expense category.
2. Enter Fare, From, and To.
3. Click Save or Done in the transportation popup or panel.
4. Confirm the particulars and amount are updated.

### Add Meal Details

1. Select a meal-related expense category.
2. Choose the applicable meals.
3. Use Dinner or OT Meal when applicable.
4. Use Paid For when the expense covers another employee.
5. Click Done.
6. Confirm the calculated amount and remarks.

### Add Allowance or Per Diem Details

1. Select Allowance when applicable.
2. Enter the required number of days or deduction days.
3. Click Done.
4. Confirm the computed amount.

### File a Report

1. Complete the ERF details.
2. Add all required expense lines.
3. Attach scanned receipts when required.
4. Select the report in the main grid.
5. Click File Report.
6. Confirm the report is ready for approval.

Once filed, the report moves into the approval workflow. Do not file until report details and expenses are complete.

### Print or Preview a Report

1. Select the report from the main grid.
2. Click Print Preview.
3. Review the report output.
4. Print only when the details are correct.

Printing and previewing require the Crystal Reports runtime on the workstation.

### View Previous ERs

1. Open Account Settings.
2. Select Previous ER.
3. Search or select the report to review.
4. Open the report details if needed.

Use Previous ER to review old expense reports, references, dates, and statuses.

### Respond to Physical Receipt Reminders

If ER System shows a physical receipt reminder after login:

1. Review the reminder message.
2. Identify the approved ERFs that still need physical receipts.
3. Submit the physical receipts to Finance.
4. Ask Finance to mark the receipts as received.

## Approver and Signatory Workflows

### Open Reports for Approval

1. Open Forms.
2. Select For Approval.
3. Review the list of pending reports.
4. Select the report to inspect.

Approval access depends on department, signatory, and authority configuration.

### Review a Submitted Report

Before taking action, review:

- Employee name and department.
- Report date range.
- Purpose of expense.
- ERF reference number.
- Cash advance or revolving fund details.
- Expense lines and totals.
- Attachments or scanned receipts.
- Remarks and supporting information.

### Approve a Report

1. Select the report.
2. Review the report details.
3. Confirm the expenses and attachments are acceptable.
4. Click Approve.
5. Confirm the action when prompted.

Approved reports continue to the next workflow step.

### Reject or Return a Report

1. Select the report.
2. Review the issue.
3. Click Reject or the applicable return action.
4. Enter a clear note when prompted.
5. Confirm the action.

Use reject or return actions when the user must correct report details, expenses, receipts, or supporting information.

### Review Previous Forms

1. Open Forms.
2. Select Previous Forms.
3. Search for the report.
4. Review status and details.

Use Previous Forms for audit checks and follow-up questions.

## Admin Workflows

### Configure Database Connection

1. Open the Connection form.
2. Select the data source.
3. Enter Servername.
4. Choose Windows Authentication or SQL Server Authentication.
5. Enter Username and Password only when SQL Server Authentication is used.
6. Enter Database.
7. Click Test Connection.
8. Click Save after the test succeeds.

Connection settings are stored under the current Windows user's ER System registry settings. Do not share production credentials in screenshots, notes, or manuals.

### Create a User Account

1. Open Account Settings.
2. Select User Account.
3. Enter User ID.
4. Enter Full Name and Position.
5. Select Department.
6. Enter Username and Password.
7. Enter Email To and Email BCC.
8. Select User Level.
9. Enter Approver 1 and Approver 2.
10. Enter Transportation, Breakfast, Lunch, Dinner, and OT Meal rates.
11. Add a signature image when required.
12. Click Save.

Supported user levels include:

- Admin
- User
- Finance

### Update a User Account

1. Open User Account.
2. Double-click the user row.
3. Update the required fields.
4. Click Update.

Verify these fields carefully:

- Department
- User Level
- Approver 1
- Approver 2
- Employee rates
- Signature
- Email To and Email BCC

### Manage Signatories

1. Open Account Settings.
2. Select Signatory.
3. Select Department.
4. Enter Endorse By.
5. Enter Review By.
6. Enter Approve By.
7. Click Add or Update.

Users may be blocked from filing or opening reports if signatory setup is incomplete.

### Manage User Authority

1. Open My Account Settings or the appropriate account settings screen.
2. Review the User Authority grid.
3. Add approver rows as needed.
4. Set Sort order.
5. Remove incorrect rows with Delete.
6. Click Save.

Authority rows affect approval routing and menu access.

### Maintain Employee Rates

1. Open the user's account settings.
2. Review Employee Rates.
3. Enter Transpo, Breakfast, Lunch, Dinner, and OT Meal rates.
4. Save changes.

Rates should be verified before the user creates or files an ERF.

## Finance Workflows

### Open Finance Review

1. Log in with a Finance account.
2. Open Forms.
3. Select Finance Review.

The Finance Review menu is visible only for accounts with the Finance user level.

### Filter the Finance Queue

Finance Review includes these filters:

- Employee
- Status
- Receipts
- ERF Type
- Date filter
- From
- To

After changing filters, click Refresh.

Common values:

- Status: Pending, Receipts Received, All
- Receipts: Missing, Received, All
- ERF Type: All, Replenishment of Revolving fund, Liquidation for Cash Advance, Reimbursement

### Review ERF Details

1. Select a row in the Finance Review grid.
2. Review the ERF Details panel.
3. Confirm the employee, description, type, date range, ERF reference number, cash reference number, cash amount, revolving fund, finance status, physical receipt status, received date, scanned receipt deletion date, and remarks.

### Mark Physical Receipts Received

1. Select the ERF row.
2. Enter Finance Remarks when needed.
3. Click Receipts Received.
4. Confirm the success message.
5. Refresh the queue.

The Receipts Received button is disabled when the selected report is already marked as received.

### Send SMS Notification

1. In Finance Review, click Send in the SMS column.
2. Review the Recipients list.
3. Select a notification template:
   - Physical receipts reminder
   - Receipts received notice
   - Finance follow-up
4. Review the message preview.
5. Click Send.

If no recipients are found, the Send button is disabled. Ask Admin to verify the user's account, contact information, and authority setup.

### Process Missing Physical Receipts

1. Open Finance Review.
2. Set Receipts to Missing.
3. Click Refresh.
4. Select the ERF.
5. Review the ERF reference number and details.
6. Send a Physical receipts reminder SMS when appropriate.
7. Wait for the user to submit physical receipts.
8. Enter remarks.
9. Click Receipts Received.

## Help and Built-In Guides

Open Help from the Help menu for image-based guidance. Available help topics may include:

- How to insert signatory.
- How to change email/password.
- How to insert expense.
- How to add cash advance.
- How to send email.
- How to file report.
- How to edit report.
- How to approve or unapprove.
- Previous ER.

If help images are missing or outdated, contact support.

## Common Issues

### Login Does Not Work

Possible causes:

- Incorrect username or password.
- Account is inactive.
- Database connection is missing or incorrect.
- Network connection to the database is unavailable.

Contact Admin if the issue continues.

### Menu Is Missing

Possible causes:

- User level does not allow the menu.
- Department setup is incomplete.
- Authority or signatory setup is incomplete.
- The user needs to log out and log back in after account changes.

### Cannot Open or File an ERF

Possible causes:

- Missing meal or transportation rates.
- Missing signatory setup.
- Missing approver setup.
- Required report fields are blank.
- Required receipts or expense lines are incomplete.

### Expense Amount Looks Incorrect

Possible causes:

- Employee rates are missing or outdated.
- Wrong category or type was selected.
- Multiplier or number of days is incorrect.
- Paid For or Work With details changed the calculation.

### Finance Review Does Not Load

Possible causes:

- Account is not Finance user level.
- Finance database migration was not applied.
- Database permissions are missing.
- Network connection to the database is unavailable.

### Print Preview Does Not Work

Possible causes:

- Crystal Reports runtime is missing.
- Crystal Reports runtime version is incompatible.
- Report data is incomplete.

## Support Information to Provide

When reporting an issue, include:

- Windows username.
- ER System username.
- User level.
- Department.
- Workstation name.
- Report ID or ERF reference number.
- Screen name.
- Exact error message.
- Steps performed before the issue occurred.
- Whether other users have the same issue.

## Good Practices

- File reports only after all details and expense lines are complete.
- Attach scanned receipts when available.
- Keep physical receipts for Finance.
- Review Print Preview before printing.
- Do not share passwords or database credentials.
- Log out when leaving the workstation.
- Ask Admin to verify rates, approvers, and signatories before first use.
