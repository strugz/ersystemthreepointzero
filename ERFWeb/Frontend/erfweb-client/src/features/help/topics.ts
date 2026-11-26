export type HelpVideoKey = 'filing' | 'approving' | 'finance' | 'verifier';

/**
 * The narrated videos (rebuilt from ERFWeb/Document/help-video, see its README.txt). Each url is a path inside the
 * SPA, built from the base path so a sub-path host still works, or a full https:// URL to a file server or CDN.
 * `hd` is the 1080p copy the player switches to when the viewer picks HD, so it is fetched only then.
 * Raise ?v= whenever a file is replaced under the same name, so browsers fetch the new copy.
 */
export const HELP_VIDEOS: Record<HelpVideoKey, { title: string; url: string; hd: { url: string; size: string } }> = {
  filing: {
    title: 'Filing a report',
    url: `${import.meta.env.BASE_URL}help/erfweb-filing-guide.mp4?v=3`,
    hd: { url: `${import.meta.env.BASE_URL}help/erfweb-filing-guide-hd.mp4?v=2`, size: '13.5 MB' },
  },
  approving: {
    title: 'Approving reports',
    url: `${import.meta.env.BASE_URL}help/erfweb-approving-guide.mp4?v=3`,
    hd: { url: `${import.meta.env.BASE_URL}help/erfweb-approving-guide-hd.mp4?v=2`, size: '10.2 MB' },
  },
  finance: {
    title: 'Finance',
    url: `${import.meta.env.BASE_URL}help/erfweb-finance-guide.mp4?v=2`,
    hd: { url: `${import.meta.env.BASE_URL}help/erfweb-finance-guide-hd.mp4?v=2`, size: '6.0 MB' },
  },
  verifier: {
    title: 'Expense Verifier',
    url: `${import.meta.env.BASE_URL}help/erfweb-verifier-guide.mp4?v=2`,
    hd: { url: `${import.meta.env.BASE_URL}help/erfweb-verifier-guide-hd.mp4?v=2`, size: '4.1 MB' },
  },
};

/** A same-site path or an https:// URL plays; anything else (empty, http:, //host, javascript:) counts as no video. */
export function safeVideoUrl(url: string | null | undefined): string | null {
  const u = (url ?? '').trim();
  if (u.startsWith('https://')) return u;
  if (u.startsWith('/') && !u.startsWith('//')) return u;
  const base = import.meta.env.BASE_URL;
  if (base && base !== '/' && u.startsWith(base) && !u.startsWith('//')) return u;
  return null;
}

export interface HelpTopic {
  /** Used in the address (`/help?topic=…`); category topics are `category-<ExpenseCategory>`. */
  id: string;
  video: HelpVideoKey;
  /** Shown as a badge on questions about one signatory level only. */
  level?: 'Level 1' | 'Level 2';
  group: string;
  question: string;
  /** Start time in the video, in seconds (help-video/chapters.json or chapters_approving.json). */
  start: number;
  answer: string[];
  keywords?: string;
}

type TopicEntry = Omit<HelpTopic, 'video'>;

const filingTopics: TopicEntry[] = [
  {
    id: 'sign-in',
    group: 'Getting started',
    question: 'How do I sign in, and what does the Dashboard show?',
    start: 8.9,
    answer: [
      'Open ERFWeb in Chrome or Edge and sign in with the username and password provided by your administrator.',
      'The Dashboard shows your drafts, the reports awaiting approval and any report returned to you.',
    ],
    keywords: 'login password dashboard home',
  },
  {
    id: 'create-report',
    group: 'Getting started',
    question: 'How do I create an expense report?',
    start: 27.8,
    answer: [
      'Select New report.',
      'Select the report type: Reimbursement (the company owes you the full amount), Liquidation of Cash Advance (you received money before your trip) or Replenishment of Revolving Fund.',
      'Enter Date from and Date to. ERFWeb fills in the Purpose, which you may edit.',
      'Select Create report.',
    ],
    keywords: 'new report type reimbursement liquidation revolving fund purpose period',
  },
  {
    id: 'report-department',
    group: 'Getting started',
    question: 'I work for more than one department. How do I file a report for the other one?',
    start: 46.0,
    answer: [
      "On New report (or Edit on Report details before you file), choose the Department this report is for. That department's approvers sign it.",
      'You see the Department field only when your administrator has allowed it for your account; otherwise every report goes to your own department.',
      'If filing says "Filing for other departments is off for your account", change the Department back to your main department.',
    ],
    keywords: 'department other department collection approver multi',
  },
  {
    id: 'draft',
    group: 'Getting started',
    question: 'What is a draft report?',
    start: 64.9,
    answer: [
      'A new report is saved as a draft. You may edit it at any time until you file it.',
      'The ERF reference number is assigned only when the report is filed.',
    ],
    keywords: 'draft erf reference number edit',
  },
  {
    id: 'add-line',
    group: 'Expense lines',
    question: 'How do I add an expense line?',
    start: 76.6,
    answer: [
      'Select Add expense line (Add line once the report has lines).',
      'Enter the Date; it must fall within the report period. Leave Local selected, or select Foreign for expenses abroad.',
      'Select the category, then fill in Particulars, Location and, if colleagues were with you, Work with.',
      'Check the Amount and select Add line.',
    ],
    keywords: 'line expense date category particulars location work with amount',
  },
  {
    id: 'after-add-line',
    group: 'Expense lines',
    question: 'What do Go to and Stay on mean after Add line?',
    start: 106.2,
    answer: [
      'The window stays open for your next expense.',
      'Go to moves the next line to the following day; Stay on keeps the same date.',
      'Same visit, other expense starts a new line for the same visit.',
    ],
    keywords: 'next date stay go to line added',
  },
  {
    id: 'same-visit',
    group: 'Expense lines',
    question: 'How do I add another expense from the same visit?',
    start: 243.7,
    answer: [
      'After Add line, select Same visit, other expense.',
      'The date, Local or Foreign, Location, Work with, service details and CRM activities are kept.',
      'Select the new category, enter the Particulars and the Amount (and the Invoice / OR no. and VAT, if any).',
    ],
    keywords: 'same visit copy parking toll another',
  },
  {
    id: 'from-crm',
    group: 'Expense lines',
    question: 'How do I fill a line from my CRM activities?',
    start: 260.6,
    answer: [
      'Before entering any details, select From CRM.',
      'Choose the day, tick each activity the expense covers and select Use activity.',
      'ERFWeb fills in the date, the client as the Location and the service details. You still enter the amount.',
      'An activity marked Already claimed is already on another report.',
      'No From CRM button? Your account is not linked to the CRM yet; ask your administrator.',
    ],
    keywords: 'crm activity service call client visit already claimed',
  },
  {
    id: 'or-vat-service',
    group: 'Expense lines',
    question: 'Where do I enter the OR number, VAT and service details?',
    start: 297.1,
    answer: [
      'Enter the official receipt number in Invoice / OR no.; the VAT amount can be entered only after it.',
      'Under Service details, enter the Instrument, Serial no., Service no. and Remarks. From CRM fills these in for you.',
    ],
    keywords: 'invoice or official receipt vat instrument serial service number remarks',
  },
  {
    id: 'review-lines',
    group: 'Expense lines',
    question: 'How do I edit or remove a line?',
    start: 316.8,
    answer: [
      'Lines appear in the Expense lines card, grouped by date and location, in the order they print on the R47.',
      'Select the pencil icon to edit a line, or the X icon to remove it.',
    ],
    keywords: 'edit remove delete change line sort order',
  },
  {
    id: 'category-Meals',
    group: 'Expense categories',
    question: 'What do I need for a Meals line?',
    start: 89.3,
    answer: [
      'Select each meal you took that day (Breakfast, Lunch, Dinner, OT Meal). The amount comes from your approved rates.',
      'Enter the Location and the colleagues under Work with.',
      'Each meal may be claimed only once per day, on any report.',
      'If a colleague paid for your meal and listed you under Work with, you cannot claim that meal.',
    ],
    keywords: 'meal breakfast lunch dinner ot rate work with',
  },
  {
    id: 'category-Transportation',
    group: 'Expense categories',
    question: 'What do I need for a Transportation line?',
    start: 120.3,
    answer: [
      'Under Fare legs, select the Mode (for example Grab), then enter From, To and the Fare for each ride.',
      'Select Use as amount to copy the legs total into Amount.',
    ],
    keywords: 'transportation fare leg grab taxi jeep bus mode',
  },
  {
    id: 'category-Toll',
    group: 'Expense categories',
    question: 'What do I need for a Toll line?',
    start: 135.3,
    answer: [
      'Describe the route in Particulars, for example "Toll - SLEX Alabang to Calamba".',
      'Paid the same toll more than once? Enter the amount once and set the Multiplier.',
    ],
    keywords: 'toll expressway slex nlex multiplier',
  },
  {
    id: 'category-Parking',
    group: 'Expense categories',
    question: 'What do I need for a Parking line?',
    start: 243.7,
    answer: [
      'Enter where you parked in Particulars and Location, and the Amount.',
      'If you have an official receipt, enter its Invoice / OR no. and the VAT amount.',
      'Parking during a client visit? Use From CRM, or Same visit, other expense after the visit\'s first line.',
    ],
    keywords: 'parking or vat',
  },
  {
    id: 'category-Lodging',
    group: 'Expense categories',
    question: 'What do I need for a Lodging line?',
    start: 149.0,
    answer: [
      'Enter the amount Per night and the number of Nights.',
      'The amount per night cannot exceed your allowed lodging rate.',
      'Attach the official receipt from the hotel first: select Upload in the yellow notice.',
    ],
    keywords: 'lodging hotel night receipt rate',
  },
  {
    id: 'category-Gas',
    group: 'Expense categories',
    question: 'What do I need for a Gas line?',
    start: 167.1,
    answer: [
      'For fuel for your company-issued vehicle.',
      'Select the Reason from the list, then enter the Vehicle model and Plate no. ERFWeb builds the Particulars from them.',
      'If you hold a fleet card, only the reasons on the list are accepted.',
    ],
    keywords: 'gas fuel gasoline diesel vehicle plate fleet card reason',
  },
  {
    id: 'category-VehicleEmergency',
    group: 'Expense categories',
    question: 'What do I need for a Vehicle emergency purchase line?',
    start: 186.1,
    answer: [
      'For an urgent repair or purchase for your company-issued vehicle.',
      'Select the Reason (for example Flat tire or Battery replacement), then enter the Vehicle model and Plate no.',
    ],
    keywords: 'vehicle emergency repair tire battery towing plate',
  },
  {
    id: 'category-Representation',
    group: 'Expense categories',
    question: 'What do I need for a Representation line?',
    start: 198.8,
    answer: [
      'For meals with clients.',
      'Attach the food or meal receipt first: select Upload in the yellow notice. Add line stays unavailable until it is attached.',
    ],
    keywords: 'representation client meal food receipt',
  },
  {
    id: 'category-Sponsorship',
    group: 'Expense categories',
    question: 'What do I need for a Sponsorship line?',
    start: 209.2,
    answer: ['Attach the approved Activity Request Form first: select Upload in the yellow notice.'],
    keywords: 'sponsorship activity request form arf',
  },
  {
    id: 'category-Demo',
    group: 'Expense categories',
    question: 'What do I need for a Demo line?',
    start: 216.4,
    answer: [
      'Attach the ISO-registered attendance sheet (form R94) first: select Upload in the yellow notice.',
      'ERFWeb checks the file for the form code; for a photo or scan it asks you to confirm.',
    ],
    keywords: 'demo demonstration attendance sheet r94',
  },
  {
    id: 'category-Others',
    group: 'Expense categories',
    question: 'When do I use Others?',
    start: 231.6,
    answer: [
      'For a work expense that fits no other category, such as a courier fee.',
      'Describe it clearly in Particulars. No document is required.',
    ],
    keywords: 'others courier miscellaneous',
  },
  {
    id: 'cash-advance',
    group: 'Cash advance and receipts',
    question: 'How do I record a cash advance?',
    start: 330.9,
    answer: [
      'For a Liquidation of Cash Advance only.',
      'Under Cash advance received, select Add advance. Enter the Date, Amount, Reference document (for example Check Voucher) and Ref. no.',
      'Select Add advance, then Done.',
    ],
    keywords: 'cash advance liquidation check voucher',
  },
  {
    id: 'totals',
    group: 'Cash advance and receipts',
    question: 'Who is the balance due to?',
    start: 348.0,
    answer: ['The top of the report shows the total expenses, the cash advance and to whom the balance is due (you or MDMPI).'],
    keywords: 'balance due total',
  },
  {
    id: 'receipts',
    group: 'Cash advance and receipts',
    question: 'How do I attach receipts?',
    start: 355.1,
    answer: [
      'In the Receipts card, select what the file is, then drop the file or tap to choose (JPEG, PNG or PDF).',
      'Keep the original receipts; Finance still needs them.',
    ],
    keywords: 'receipt upload attach photo scan',
  },
  {
    id: 'checklist',
    group: 'Filing',
    question: 'What must be complete before I file?',
    start: 370.0,
    answer: [
      'Review the Before you file checklist and resolve any item that is not checked. The signature item is optional.',
      'The checklist also shows the Friday on which your report will be processed.',
    ],
    keywords: 'before you file checklist cutoff friday signature',
  },
  {
    id: 'file-report',
    group: 'Filing',
    question: 'How do I file the report?',
    start: 384.3,
    answer: [
      'Select File report. ERFWeb shows the total and the approver.',
      'Select File report again to submit it. Once filed, the expenses can no longer be changed.',
    ],
    keywords: 'file submit send approval',
  },
  {
    id: 'after-filing',
    group: 'Filing',
    question: 'What happens after I file?',
    start: 397.0,
    answer: [
      'The report appears under My reports with its ERF reference number and the status For Approval.',
      'ERFWeb notifies you by text message and email when it is approved or returned.',
    ],
    keywords: 'status for approval approved returned my reports notification',
  },
];

const approvingTopics: TopicEntry[] = [
  {
    id: 'approve-dashboard',
    group: 'Finding reports',
    question: 'Where do I see the reports waiting for me?',
    start: 8.8,
    answer: [
      'The Dashboard shows a For your approval card when reports wait for you, with the employee, reference no. and whether it is your turn.',
      'Select All approvals, or Approvals in the menu, to see them all.',
    ],
    keywords: 'dashboard for your approval inbox pending',
  },
  {
    id: 'approve-inbox',
    group: 'Finding reports',
    question: 'What do Your turn and Waiting for mean?',
    start: 24.0,
    answer: [
      'To review lists the reports routed to you. Your turn means you can act now; Waiting for means an earlier step must act first.',
      'Search by employee, purpose or reference no., and use Export CSV to download the list. Select a report to open it.',
    ],
    keywords: 'approvals to review your turn waiting search export csv',
  },
  {
    id: 'approve-review',
    group: 'Reviewing a report',
    question: 'What is on the review page?',
    start: 45.2,
    answer: [
      'The totals, the report details, the expense lines, the CRM calendar, the approval steps and the receipts.',
      'Your actions are at the top right: R47 PDF, Reject, Return and Approve.',
    ],
    keywords: 'review page totals details lines',
  },
  {
    id: 'approve-other-department',
    group: 'Reviewing a report',
    question: 'Why did I get a report from someone outside my department?',
    start: 45.2,
    answer: [
      "Some employees work for more than one department and charge a report to the department it is for. That department's approvers sign it.",
      'The review page shows the department next to the report type, and Report details shows it as "other department".',
    ],
    keywords: 'department other department charged employee outside',
  },
  {
    id: 'approve-receipts',
    group: 'Reviewing a report',
    question: 'How do I check the receipts against the lines?',
    start: 84.4,
    answer: [
      'Select Check receipts side by side. The lines are on the left and the receipt on the right.',
      'Use the up and down arrow keys to move between lines and the left and right arrow keys to move between receipts. You can zoom, rotate, open or download a receipt, and Return or Approve from this window.',
    ],
    keywords: 'receipt check side by side zoom rotate',
  },
  {
    id: 'approve-crm',
    group: 'Reviewing a report',
    question: 'How do I use the CRM calendar?',
    start: 105.5,
    answer: [
      "Before you approve, compare the employee's CRM activities and leave, day by day, with the expense lines.",
      'A warning appears when an expense line falls on a leave or holiday day.',
    ],
    keywords: 'crm calendar activities leave holiday visit warning',
  },
  {
    id: 'approve-steps',
    group: 'Reviewing a report',
    question: 'Where do I see the approval steps?',
    start: 126.2,
    answer: ['The Approval card shows each step in order with its status. The Receipts card lists the files the employee attached.'],
    keywords: 'approval steps timeline status receipts',
  },
  {
    id: 'approve-approve',
    group: 'Deciding',
    question: 'How do I approve a report?',
    start: 135.3,
    answer: [
      'Select Approve, add a note if you wish, and select Approve again.',
      'Line amounts lock once the report has its first approval. After the Level 1 approves, the report moves to the Level 2.',
    ],
    keywords: 'approve note',
  },
  {
    id: 'approve-return',
    group: 'Deciding',
    question: 'How do I return a report for correction?',
    start: 148.2,
    answer: [
      'Select Return and state what the employee should fix (required).',
      'The employee corrects the report and files it again; approval restarts from the first approver.',
    ],
    keywords: 'return correction fix refile',
  },
  {
    id: 'approve-reject',
    group: 'Deciding',
    question: 'When should I reject instead of return?',
    start: 160.6,
    answer: [
      'Reject only when the report must not be paid at all. The reason is required and rejection is final: the report cannot be filed again.',
      'Use Return whenever the employee can correct the report.',
    ],
    keywords: 'reject final reason',
  },
  {
    id: 'approve-r47',
    group: 'Deciding',
    question: 'Can I approve from the R47 PDF?',
    start: 174.5,
    answer: ['Yes. Select R47 PDF on the review page; the form shows exactly as it prints, with Approve at the top right.'],
    keywords: 'r47 pdf print approve',
  },
  {
    id: 'approve-acted',
    group: 'Deciding',
    question: 'Where do I see the reports I already acted on?',
    start: 185.3,
    answer: ['The Acted on tab on the Approvals page lists the reports you approved, returned or rejected, with your action and its date.'],
    keywords: 'acted on history',
  },
  {
    id: 'approve-lower',
    level: 'Level 1',
    group: 'Level 1 and Level 2',
    question: 'How do I lower a line amount?',
    start: 193.4,
    answer: [
      'Only the current Level 1, before the first approval, can lower a line.',
      'Select the lower amount icon on the line, or Lower this amount in the receipt check. Enter the new amount, pick or type a reason, and select Lower amount.',
      'The employee sees the reason, and the filed amount stays on record.',
    ],
    keywords: 'lower amount reduce reason above the allowed rate',
  },
  {
    id: 'approve-l2-inbox',
    level: 'Level 2',
    group: 'Level 1 and Level 2',
    question: 'What does You can override mean?',
    start: 214.4,
    answer: [
      'A Level 2 sees a report after its Level 1 approves it, marked Your turn.',
      'A report still waiting for its Level 1 is marked You can override.',
    ],
    keywords: 'level 2 you can override waiting',
  },
  {
    id: 'approve-override',
    level: 'Level 2',
    group: 'Level 1 and Level 2',
    question: 'How do I approve without the Level 1?',
    start: 225.1,
    answer: [
      'Open the report, select Override, state why, and select Override and approve.',
      'The Level 1 step is marked Overridden, and the reason is kept in the audit trail.',
    ],
    keywords: 'override level 1 on leave',
  },
  {
    id: 'approve-final',
    group: 'Level 1 and Level 2',
    question: 'What happens after the final approval?',
    start: 242.5,
    answer: [
      'The report is approved and goes to Finance, which waits for the original receipts.',
      'Gas and vehicle emergency lines may also be checked by an Expense Verifier.',
    ],
    keywords: 'final approval finance receipts verifier',
  },
  {
    id: 'approve-queries',
    level: 'Level 2',
    group: 'Level 1 and Level 2',
    question: 'How do I decide on a Finance query?',
    start: 257.1,
    answer: [
      'When an Expense Verifier cannot verify a line, Finance may ask the Level 2 to decide. The query appears under Queries.',
      'Open the report, select Decide, and choose Let it through, Return for correction or Reject the report.',
    ],
    keywords: 'queries decide verification finance',
  },
  {
    id: 'approve-delegate',
    group: 'Leave and reports',
    question: 'How do I name a delegate while I am on leave?',
    start: 274.4,
    answer: [
      'On Delegations, select New delegation, choose a delegate of your level or higher, set the dates and a reason, and select Save.',
      'The delegate approves, returns or rejects in your place, stamped "on behalf of" you. A delegation lasts at most 90 days; rates and scope are never delegated.',
    ],
    keywords: 'delegation delegate leave on behalf',
  },
  {
    id: 'approve-delegate-l1',
    level: 'Level 2',
    group: 'Leave and reports',
    question: 'How do I name a delegate for a Level 1?',
    start: 301.5,
    answer: ['In New delegation, choose A Level 1 I am over, then select the Level 1 and the delegate.'],
    keywords: 'delegation level 1 i am over',
  },
  {
    id: 'approve-budgets',
    group: 'Leave and reports',
    question: 'What does the Budgets page show?',
    start: 311.6,
    answer: [
      "Each department's yearly budget against what its employees spent, for the departments in your scope. It is for monitoring only.",
      'If no cost center is shown, ask the System Administrator to set one up.',
    ],
    keywords: 'budgets cost center monitoring',
  },
  {
    id: 'approve-summary',
    group: 'Leave and reports',
    question: 'How do I see and export a summary of expenses?',
    start: 326.7,
    answer: [
      'On Summary, choose Reports I sign for, set the dates and the department, or use This month or Last month.',
      'Select Export for a CSV file (Excel) or a PDF.',
    ],
    keywords: 'summary export csv pdf reports i sign for',
  },
  {
    id: 'approve-notifications',
    group: 'Leave and reports',
    question: 'Which notifications do I receive?',
    start: 348.4,
    answer: [
      'A text message and an email when a report is filed for you.',
      'Where reminders are enabled, a reminder when a report waits too long.',
    ],
    keywords: 'notification sms email reminder',
  },
];

const financeTopics: TopicEntry[] = [
  {
    id: 'finance-queue',
    group: 'Receipts and clearing',
    question: 'What is on the Finance queue?',
    start: 16.0,
    answer: [
      'Every fully approved report waiting for its original receipts. The Aging card counts open reports by days since final approval.',
      'Search by employee, purpose or reference no., filter by receipt status, and turn on Include cleared for finished reports. Export CSV and Export aging download the lists.',
    ],
    keywords: 'finance queue aging export csv include cleared',
  },
  {
    id: 'finance-receipts',
    group: 'Receipts and clearing',
    question: 'How do I record the original receipts?',
    start: 37.2,
    answer: [
      "Select Receipts on the report's row.",
      'Choose Receipts received, or Receipts incomplete and say which receipts are missing, then select Save.',
      'Receipts received is one-time and cannot be changed afterwards.',
    ],
    keywords: 'receipts received incomplete original paper',
  },
  {
    id: 'finance-follow-up',
    group: 'Receipts and clearing',
    question: 'How do I remind an employee to submit receipts?',
    start: 54.5,
    answer: [
      "Select Follow up on the report's row. ERFWeb prepares a text message to the employee, also sent to the report's Level 1 and Level 2.",
      'Edit the message if needed, then select Send. The row counts the follow-ups.',
    ],
    keywords: 'follow up sms text reminder receipts',
  },
  {
    id: 'finance-clear',
    group: 'Receipts and clearing',
    question: 'How do I clear a report for payment?',
    start: 69.1,
    answer: [
      'After Receipts received, select Clear for payment, add a remark if needed, and confirm.',
      'Clearing is final. The report then shows only with Include cleared on.',
      'A report under verification can be cleared only after the verification is resolved.',
    ],
    keywords: 'clear payment final cleared',
  },
  {
    id: 'finance-open-report',
    group: 'Verification',
    question: 'How do I look at a report in full?',
    start: 87.9,
    answer: ['Select the report. You see the totals, the details, the expense lines and the receipts the employee uploaded.'],
    keywords: 'open report lines receipts details',
  },
  {
    id: 'finance-verify',
    group: 'Verification',
    question: 'How do I ask for a line to be verified?',
    start: 97.0,
    answer: [
      "On the report's Verification card, select Verify a line.",
      'Choose the line, write what Finance doubts, and select Send to verifiers. The Expense Verifiers get a text and an email.',
    ],
    keywords: 'verify line verification expense verifier doubt',
  },
  {
    id: 'finance-verification-progress',
    group: 'Verification',
    question: 'How do I follow a verification?',
    start: 116.7,
    answer: [
      'The Verification card shows the request as a conversation: asked, picked up and the finding, with whose turn it is.',
      'You get a text and an email when the finding is recorded.',
    ],
    keywords: 'verification progress finding picked up',
  },
  {
    id: 'finance-under-verification',
    group: 'Verification',
    question: 'Why does a report say Under verification?',
    start: 116.7,
    answer: [
      'A verification request on one of its lines is not resolved yet, so the report cannot be cleared.',
      'Gas and Vehicle emergency purchase lines get a request automatically when the report is fully approved.',
    ],
    keywords: 'under verification gas vehicle automatic',
  },
  {
    id: 'finance-signatures',
    group: 'Signatures and reports',
    question: 'How do I approve a signature?',
    start: 146.6,
    answer: [
      'Open Signatures. New signatures wait under Waiting for approval; approve each one or reject it (a reason is optional).',
      'A signature prints on the R47 only after approval, always with the electronic signature watermark. Another Finance user approves your own.',
    ],
    keywords: 'signatures approve reject watermark r47',
  },
  {
    id: 'finance-meal-claims',
    group: 'Signatures and reports',
    question: 'How do I check daily meal claims?',
    start: 165.6,
    answer: ['Open Meal claims, choose the dates and the department or Approved only, and select Export for the list.'],
    keywords: 'meal claims daily breakfast lunch dinner export',
  },
  {
    id: 'finance-summary',
    group: 'Signatures and reports',
    question: 'How do I export an expense summary?',
    start: 176.3,
    answer: ['Open Summary, set the dates and the department, then select Export for a CSV or PDF file.'],
    keywords: 'summary export csv pdf totals',
  },
  {
    id: 'finance-budgets',
    group: 'Signatures and reports',
    question: 'Where do I see the budgets?',
    start: 189.2,
    answer: ["The Budgets page compares each department's yearly budget with what its employees spent. It is for monitoring only."],
    keywords: 'budgets cost center monitoring',
  },
];

const verifierTopics: TopicEntry[] = [
  {
    id: 'verifier-queue',
    group: 'Verification requests',
    question: 'Where do verification requests come from?',
    start: 12.1,
    answer: [
      'Finance asks for a line to be verified, or a Gas or Vehicle emergency purchase line gets a request automatically when the report is fully approved.',
      'Every Expense Verifier gets a text and an email, and the request waits under Open on the Verifications page.',
      'Requests on your own reports, or on reports you approved, are left out.',
    ],
    keywords: 'verifications open in progress my findings queue request',
  },
  {
    id: 'verifier-report',
    group: 'Verification requests',
    question: 'Can I change the amount or approve the report?',
    start: 40.2,
    answer: ['No. The report opens read-only. You only report what you find; Finance and the signatories decide.'],
    keywords: 'read only amount approve return change',
  },
  {
    id: 'verifier-pick-up',
    group: 'Verification requests',
    question: 'How do I start checking a request?',
    start: 53.2,
    answer: [
      'Open the request, then select Pick up on the Verification card.',
      'The request moves to In progress, so the other verifiers know you are checking it. Picking up sends no message.',
    ],
    keywords: 'pick up start in progress claim',
  },
  {
    id: 'verifier-check',
    group: 'Checking and findings',
    question: 'How do I check the receipts?',
    start: 73.3,
    answer: [
      'Select Check receipts side by side on the Expense lines card.',
      'Compare the date, the invoice or OR number and the amount with the line, and check anything else Finance asked about.',
    ],
    keywords: 'receipts side by side check facts or invoice',
  },
  {
    id: 'verifier-finding',
    group: 'Checking and findings',
    question: 'How do I record my finding?',
    start: 94.0,
    answer: [
      'Select Record finding on the Verification card.',
      'Choose Verified, Verified with note or Not verified, write what you found, then select Record finding.',
      'A finding is recorded once and cannot be changed.',
    ],
    keywords: 'record finding verified with note not verified',
  },
  {
    id: 'verifier-after',
    group: 'Checking and findings',
    question: 'What happens after my finding?',
    start: 114.4,
    answer: [
      'Finance gets a text and an email.',
      'A Verified finding closes the request and Finance can clear the report.',
      'After Not verified, Finance refers the line to the Level 2 signatory, who lets it through, returns the report or rejects it.',
    ],
    keywords: 'after finding level 2 query closed clear',
  },
  {
    id: 'verifier-my-findings',
    group: 'Checking and findings',
    question: 'Where do I see the requests I finished?',
    start: 136.4,
    answer: ['Open Verifications and select My findings. Each request shows your finding, your note and how it ended.'],
    keywords: 'my findings history done',
  },
];

export const helpTopics: HelpTopic[] = [
  ...filingTopics.map((t) => ({ ...t, video: 'filing' as const })),
  ...approvingTopics.map((t) => ({ ...t, video: 'approving' as const })),
  ...financeTopics.map((t) => ({ ...t, video: 'finance' as const })),
  ...verifierTopics.map((t) => ({ ...t, video: 'verifier' as const })),
];

export function formatStart(seconds: number): string {
  const s = Math.floor(seconds);
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}
