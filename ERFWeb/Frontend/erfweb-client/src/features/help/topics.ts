/** The narrated filing video; rebuilt from ERFWeb/Document/help-video (see its README.txt). */
export const HELP_VIDEO_SRC = '/help/erfweb-filing-guide.mp4';

export interface HelpTopic {
  /** Used in the address (`/help?topic=…`); category topics are `category-<ExpenseCategory>`. */
  id: string;
  group: string;
  question: string;
  /** Start time in the video, in seconds (from help-video/chapters.json). */
  start: number;
  answer: string[];
  keywords?: string;
}

export const helpTopics: HelpTopic[] = [
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
    id: 'draft',
    group: 'Getting started',
    question: 'What is a draft report?',
    start: 50.3,
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
    start: 62.0,
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
    start: 91.6,
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
    start: 229.1,
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
    start: 246.0,
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
    start: 282.5,
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
    start: 302.1,
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
    start: 74.7,
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
    start: 105.7,
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
    start: 120.7,
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
    start: 229.1,
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
    start: 134.4,
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
    start: 152.5,
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
    start: 171.5,
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
    start: 184.2,
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
    start: 194.6,
    answer: ['Attach the approved Activity Request Form first: select Upload in the yellow notice.'],
    keywords: 'sponsorship activity request form arf',
  },
  {
    id: 'category-Demo',
    group: 'Expense categories',
    question: 'What do I need for a Demo line?',
    start: 201.8,
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
    start: 217.0,
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
    start: 316.3,
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
    start: 333.4,
    answer: ['The top of the report shows the total expenses, the cash advance and to whom the balance is due (you or MDMPI).'],
    keywords: 'balance due total',
  },
  {
    id: 'receipts',
    group: 'Cash advance and receipts',
    question: 'How do I attach receipts?',
    start: 340.5,
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
    start: 355.4,
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
    start: 369.7,
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
    start: 382.4,
    answer: [
      'The report appears under My reports with its ERF reference number and the status For Approval.',
      'ERFWeb notifies you by text message and email when it is approved or returned.',
    ],
    keywords: 'status for approval approved returned my reports notification',
  },
];

export function formatStart(seconds: number): string {
  const s = Math.floor(seconds);
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}
