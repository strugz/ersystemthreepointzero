SCENES = [
    ("title", "System Administrator: setting up ERFWeb",
     "This video explains the System Administrator's work in ERFWeb: user accounts and roles, who signs for whom, "
     "expense rates, master data, the workflow rules, the R47 designer and the audit log."),
    ("ad-01-dashboard.jpg", "Your menu",
     "As System Administrator you see two extra groups in the menu. Signatory structure holds Scope and Rates. "
     "Administration holds Users, Master data, Rules and Audit. You can also file your own reports like any employee."),

    ("ad-02-users.jpg", "Users",
     "The Users page lists every account with its department, roles, signature and status. "
     "Search by name, username or email, or filter by department, role and status. Select an account to open it."),
    ("ad-03-new-user.jpg", "New user",
     "Select New user. Enter the username and a temporary password, at least eight characters, and share it with the employee yourself. "
     "Link the CRM employee so their CRM activities can be claimed. Then enter the full name, position and job grade, "
     "the email and the mobile number used for text messages."),
    ("ad-04-new-user-more.jpg", "New user: department and options",
     "Choose the department. Reviewed by prints in the R47's Reviewed by box; leave it blank to use the department or Rules default. "
     "Switch on Available as a Work With person, Has a fleet card, and Can file reports for other departments, then pick those departments. "
     "Select Create account. Every account starts as an Employee."),
    ("ad-05-user.jpg", "The account page",
     "The account page shows the profile, a summary of roles and signature, and the signature card. "
     "Select Edit to change the profile. Upload signature adds a signature image; it prints on the R47 only after Finance approves it."),
    ("ad-06-roles.jpg", "Roles",
     "The Roles card lists the roles granted beyond Employee. A granted role applies at once, and a role can be revoked the same way."),
    ("ad-07-grant-role.jpg", "Grant a role",
     "Select Grant role, choose Signatory Level 1, Signatory Level 2, Finance, Expense Verifier or System Administrator, "
     "add a note if you wish, and select Grant. A signatory also needs a scope before reports route to them."),
    ("ad-08-reset-password.jpg", "Reset password",
     "Select Reset password to set a temporary password. Share it with the employee directly; it is not emailed. "
     "Resetting the password also unlocks an account locked after failed sign-ins."),
    ("ad-09-deactivate.jpg", "Deactivate an account",
     "Select Deactivate when someone leaves. They can no longer sign in, but their reports and approvals stay on record. "
     "Activate brings the account back."),

    ("ad-10-scope.jpg", "Scope: who signs for whom",
     "The Scope page decides who signs for whom. Every employee needs exactly one Level 1 and one Level 2 signatory. "
     "Each row shows the signatory, the level, whom they cover, any approval limit and the period. Use the pencil to edit an assignment; "
     "to end one, set its Effective to date."),
    ("ad-11-new-assignment.jpg", "New scope assignment",
     "Select New assignment. Choose the signatory and the level. Then choose whom they cover: a whole department, named people, "
     "or a department except named people. Add an approval limit if needed, set the dates, and select Create."),
    ("ad-12-coverage.jpg", "Coverage",
     "The Coverage tab checks every active employee on a date. A Problem badge means nobody covers that person at Level 1 or Level 2, "
     "so their reports cannot be filed. Turn on Only problems to see just those people, then fix them on the Assignments tab."),

    ("ad-13-rate-profiles.jpg", "Rate profiles",
     "The Rates page lists the rate profiles, such as meals or lodging rates. Select New profile to add one, or open a profile to manage it."),
    ("ad-14-rate-lines.jpg", "Rate lines",
     "A profile holds rate lines: the category, the amount, the rule and the effective dates. "
     "Use New version for a rate change; it closes the current rate the day before. Use Edit only to correct a mistake. "
     "Filed reports keep their amounts."),
    ("ad-15-rate-line-dialog.jpg", "Add a rate line",
     "Select Add line. Choose the category and, for meals, which meals. Choose the rate type and enter the amount, "
     "an optional frequency cap, whether a receipt is required, and the effective dates."),
    ("ad-16-rate-assignments.jpg", "Assignments",
     "The Assignments card shows who the profile applies to. For each category, the most specific assignment wins: "
     "a person before a job grade, a job grade before a department, and a department before the whole company."),
    ("ad-17-assign-profile.jpg", "Assign a profile",
     "Select Assign profile. Choose Individual, Job grade, Department or Company-wide, pick the target, and set the dates. "
     "An open assignment for the same target is ended automatically."),
    ("ad-18-who-gets-what.jpg", "Check who gets what",
     "To check a person's rates, use Check who gets what. Pick the person and a date, and select Check. "
     "ERFWeb lists each rate and the profile it comes from."),

    ("ad-19-departments.jpg", "Master data: departments",
     "Master data holds the lists shared by reports and the R47. Deactivate an entry rather than delete it, so older reports keep it. "
     "Departments have a code, a name, and the Reviewed by text for their R47."),
    ("ad-20-cost-centers.jpg", "Cost centers",
     "Each department has one active cost center with an overall budget for the year. Budgets are for monitoring only: "
     "they never block filing or approval."),
    ("ad-20b-cost-center-dialog.jpg", "Add a cost center",
     "Select Add cost center, enter the code and name, choose the department, and enter this year's budget. Earlier years are kept."),
    ("ad-21-job-grades.jpg", "Job grades",
     "Job grades are used on the user form and for grade rate assignments. The code is fixed once created; the name can change."),
    ("ad-22-sites.jpg", "Sites",
     "Sites lists the company's sites, when you need them."),
    ("ad-23-fare-modes.jpg", "Fare modes",
     "Fare modes are the transport choices in a fare leg, in the order shown, with a note when a receipt is expected."),
    ("ad-24-currencies.jpg", "Currencies",
     "Currencies sets the currencies in use and the default one."),
    ("ad-25-categories.jpg", "Expense categories",
     "Expense categories are fixed by the R47 layout and read-only. The list shows which R47 column each category prints in."),

    ("ad-26-rules-approvals.jpg", "Rules: approvals",
     "The Rules page sets the workflow. Changes apply to reports filed from now on; select Save rules to keep them, or Discard changes. "
     "On the Approvals tab, set the Level 2 threshold or always require Level 2, and how reports for other departments are signed and rated."),
    ("ad-27-rules-approvals-2.jpg", "Service levels and reminders",
     "Set the service-level days for Level 1 and Level 2 and when overdue approvals escalate. "
     "The SLA reminders card shows when the daily reminders run and the result of the last run."),
    ("ad-28-rules-filing.jpg", "Rules: filing",
     "On the Filing and Finance tab, set how old an expense may be, the amount above which a receipt is required, "
     "and the cooling period before a new account can file."),
    ("ad-29-rules-cutoff.jpg", "Finance rules: cutoff and documents",
     "The Finance rules set the weekly processing cutoff, and whether a late report is only noticed or also flagged for Finance. "
     "They also switch Sponsorship and Demo on or off and set the documents they need, such as the attendance sheet form code."),
    ("ad-31-rules-lodging.jpg", "Finance rules: lodging, gas and vehicle",
     "Lodging takes its rate from the rate profile or a flat amount, can refuse a higher amount per night, and can require the hotel receipt. "
     "Gas and vehicle emergency purchases can be checked by an Expense Verifier, by a document, or both."),
    ("ad-33-rules-fleet.jpg", "Finance rules: fleet card and Work With meals",
     "Fleet card holders claim gas only for the reasons on this list. Work With meals stops a tagged colleague from claiming "
     "the same meal that the filer paid for."),
    ("ad-35-rules-r47.jpg", "Rules: R47",
     "On the R47 tab, set the default Reviewed by text and how signatures print: e-signature images with a watermark, or status only."),
    ("ad-36-rules-r47-format.jpg", "Format of the R47",
     "Choose the PDF engine and the header details: company name, form title, document code, effectivity date and revision number. "
     "Open the R47 designer from here."),
    ("ad-37-rules-notifications.jpg", "Notifications and sign-in",
     "On the last tab, switch email and text messages on or off and set the sender address. "
     "Sign-in security sets how many failed sign-ins lock a username and for how long."),
    ("ad-38-rules-signin.jpg", "Account email domains",
     "Account email domains lists the domains offered on the user form; the first one is the default for new accounts."),

    ("ad-40-r47-designer-grid.jpg", "R47 designer",
     "The R47 designer sets the layout of the form: margins, header, grid, totals, cash advance, footer, type and colours. "
     "Nothing changes until you save."),
    ("ad-39-r47-designer.jpg", "Column widths and sizes",
     "Open a section to set exact sizes, or drag a divider in the drawing to move width between two columns."),
    ("ad-41-r47-preview.jpg", "Preview the R47",
     "Select Preview PDF to print the sample report, or a real report, with your unsaved layout and either engine. "
     "When it looks right, select Save layout. Reset to standard brings back the original form."),

    ("ad-42-audit.jpg", "Audit log",
     "The Audit log records who did what and when. Entries cannot be changed. Filter by event and dates, "
     "or show only administration changes, and expand a row to see its details."),
    ("end", "Thank you",
     "For complete details, refer to the ERFWeb System Administrator Guide. Thank you for watching."),
]
