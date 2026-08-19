import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = "C:/Users/JayBryanCAbaoag/Documents/VuexJaysWayFile/VuexJaysWayFile/Project/ER System/ER System 3.0/outputs/ERF System Introduction with Updated Short Voice-Over Notes.pptx";
const output = "C:/Users/JayBryanCAbaoag/Documents/VuexJaysWayFile/VuexJaysWayFile/Project/ER System/ER System 3.0/outputs/ERF System Introduction with Detailed Voice-Over Notes.pptx";
const qaDir = "C:/Users/JayBryanCAbaoag/Documents/VuexJaysWayFile/VuexJaysWayFile/Project/ER System/ER System 3.0/.codex-tmp/voiceover-update/final-render";

const notes = [
  `Welcome to the ERF System introduction. In this walkthrough, we will follow an expense report from the employee’s initial setup through expense entry, manager review, and Finance receipt tracking. The desktop application supports the established ERF workflow, while each user sees the functions allowed for their role. As we move through the screens, pay close attention to the selected employee, department, report, and reference number. Those details help prevent work from being recorded against the wrong transaction. Use this presentation together with your organization’s current expense and approval policies, and pause on any screen when you need more time to compare the instructions with the application.`,

  `The ERF workflow begins when the user signs in with an assigned account. Before creating a report, the employee should confirm the account information, password and email setup, and approval signatory. The employee then creates the report header, records each expense, reviews the result, and files the report. Filing sends the transaction into the manager’s approval process. The manager opens the approval queue, reviews the selected employee and report, and records the appropriate decision. After approval, the completed report can be sent or printed, and Finance can track the ERF together with the physical receipts. The key control throughout the workflow is simple: verify the current user, report, and status before every save or action.`,

  `Start at the login window. Enter the username assigned to you, then enter the matching password. Password characters are hidden, so check for typing errors and make sure Caps Lock is not affecting the entry. Select Login to continue. The system validates the account and loads the user information and access level associated with that account. Do not use another employee’s credentials, because the signed-in identity is used throughout report creation and approval. If the login is unsuccessful, recheck both fields before trying again or contact the appropriate system administrator. Select Cancel only when you intend to close the login window without entering the ERF System.`,

  `After a successful login, the main page becomes the starting point for ERF work. First, confirm the signed-in user and department shown in the status area. This context is important because the system uses the current account when loading reports, approval assignments, and role-based options. The report list is the working area for locating and opening transactions. Use the top menu or the available shortcut commands to create report details, open expenses, search, file, preview, or access account settings. Some buttons appear or become enabled only after a report is selected. Before opening or editing anything, confirm that the highlighted row is the report you intend to work on.`,

  `Open My Account Settings from the Account Settings menu. This page loads the profile connected to the signed-in user. Review the displayed user and employee information carefully, including the organizational details used by the ERF process. Only fields intended for self-service editing should be changed; protected identity and assignment data should remain consistent with the official employee record. Correct account information supports accurate report ownership and notifications. If a value appears incorrect but cannot be edited, coordinate with the responsible administrator instead of working around it. When the permitted changes are complete, review them once more and select Save. Wait for the confirmation message before closing the window.`,

  `The Change Password and Email Setup window contains two related account-maintenance areas. On the Change Password tab, enter the new password and its confirmation exactly as required. The system will not accept the change when the two entries do not match, so review both fields before selecting Change. On the Email Setup tab, verify the work email account and the notification recipients, including the To and BCC values used by the existing ERF email process. Update the stored information only when you are authorized and the values have been confirmed. Select Update to save the email configuration. The application may need to close after an email update, so finish any unsaved work first.`,

  `Next, open Signatory from Account Settings. The signatory setup defines the people used in the report’s approval route. Review the displayed assignments and select the appropriate endorser, reviewer, or approver required for your department and workflow. The correct route may contain more than one approval role, so do not select a name based only on convenience or availability. Confirm the spelling and role of each person before saving. The ERF System checks for signatory information when a report is opened or filed and may stop the workflow when the required route is missing. If you are uncertain about the correct assignment, verify it with your department before continuing.`,

  `To begin a new report, return to the main workspace and open the report-details command. Complete the report header before entering individual expenses. Use the supporting documents to enter the correct reporting period, description, report type, and any other required reference or transaction details shown on the form. These header values identify the purpose and coverage of the entire expense report, so they must be consistent with the receipts that will be added later. Review the start and end dates and make sure the description is clear enough for the manager and Finance to understand the request. Save or continue only after the header information is complete.`,

  `In the expense-entry form, add each expense as a separate and accurate line. Enter the transaction date, expense description, amount, and every other field required for that expense type. Use the receipt or supporting document as the source rather than relying on memory. Where the form asks for classifications, references, or additional information, choose values that match the actual transaction. Save the item, then review the new line in the expense list to confirm that the date, description, and amount were recorded correctly. Repeat the process for all expenses. Before leaving the form, check for missing items, duplicates, incorrect totals, and entries that belong to a different reporting period.`,

  `Managers begin in Approval Review. The first list identifies employees with reports available for review. Select the correct employee, then choose the intended report from that employee’s report list. The action buttons become available only after a valid report selection. Open or review the expense details and compare the employee, report description, dates, amounts, and available supporting information. If necessary, inspect the report in detail before deciding. Avoid approving directly from a summary row without confirming the underlying transaction. The selected report context is carried into the approval action, so pause and verify the employee name and report identifier whenever you move between rows.`,

  `After completing the review, choose the action that matches the result. Select Approve only when the report is complete, accurate, supported, and ready to continue through the workflow. Use Reject when the report must be returned or cannot proceed; the system opens the cancellation or rejection note so the reason can be recorded for the employee. Enter a clear, useful explanation rather than a vague comment. Cancel the current selection if you are not ready to decide. After the action is submitted, check the confirmation and refreshed status to make sure the decision was recorded against the intended report. Do not repeat the action if the system has already processed it.`,

  `Use Print Preview to inspect the completed expense report before producing or distributing it. Confirm the employee, reporting period, expense lines, totals, approval information, and reference details in the generated document. If any information is wrong, return to the appropriate ERF screen and correct it before sending or printing. When the report is ready, select the required output method and verify the recipient, email context, printer, and number of copies. Treat printed and emailed reports as official business documents and follow current handling rules for expense and personal information. Complete the output action only after the preview matches the transaction selected on the main page.`,

  `Finance opens Finance Review from the Forms menu. This option is shown for the Finance user level. Use the status, receipt, employee, date, and report-type filters to narrow the queue, then select the correct ERF. The detail panel displays the employee, description, type, reporting dates, ERF and cash references, amount information, Finance status, physical-receipt status, received date, scanned-receipt deletion date, and existing remarks when available. Compare these details with the physical documents in hand. Add a Finance remark when it provides useful tracking context. Select Receipts Received only after the physical receipts have actually been verified; the button is disabled once the report is already marked received.`,

  `The SMS column in Finance Review provides a Send button for each report. Select Send on the correct row to open the notification window. The system resolves the available recipients associated with the ERF user and approval information. Review every recipient before continuing; if no recipients are found, sending is disabled. Choose the message that matches the situation: Physical receipts reminder, Receipts received notice, or Finance follow-up. The system builds a preview using the user and the best available ERF reference. Read the entire preview and edit it only when necessary and authorized. Select Send once. Then review the result message to confirm whether delivery succeeded or whether any recipients failed.`,

  `This completes the ERF System introduction. Employees are responsible for maintaining valid account and routing information, creating the report, entering accurate expenses, reviewing the final transaction, and filing it for approval. Managers are responsible for selecting the correct employee and report, examining the supporting details, and recording a clear approval or rejection decision. Finance is responsible for reviewing approved ERFs, tracking physical receipts, recording useful remarks, and sending the appropriate follow-up notification. Across every role, the safest habit is to verify the current user, department, report reference, selected row, and workflow status before acting. Continue to follow your organization’s current expense, approval, receipt, and records-handling policies.`,
];

const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
if (presentation.slides.items.length !== notes.length) {
  throw new Error(`Expected ${notes.length} slides but found ${presentation.slides.items.length}.`);
}

for (const [index, slide] of presentation.slides.items.entries()) {
  slide.speakerNotes.textFrame.setText(notes[index]);
  slide.speakerNotes.setVisible(true);
}

await fs.mkdir(qaDir, { recursive: true });
for (const [index, slide] of presentation.slides.items.entries()) {
  const number = String(index + 1).padStart(2, "0");
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  await fs.writeFile(`${qaDir}/slide-${number}.png`, new Uint8Array(await png.arrayBuffer()));
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(`${qaDir}/slide-${number}.layout.json`, await layout.text(), "utf8");
}

const montage = await presentation.export({ format: "webp", montage: true, scale: 1 });
await fs.writeFile(`${qaDir}/montage.webp`, new Uint8Array(await montage.arrayBuffer()));
const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(output);

const check = await presentation.inspect({
  kind: "slide,notes",
  include: "id,slide,title,text",
  maxChars: 100000,
});
await fs.writeFile(`${qaDir}/final-notes.ndjson`, check.ndjson, "utf8");
console.log(output);
