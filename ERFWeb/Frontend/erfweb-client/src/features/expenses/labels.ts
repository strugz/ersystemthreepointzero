import type {
  ApprovalStatus,
  Attachment,
  AttachmentDocumentType,
  ExpenseCategory,
  ExpenseType,
  FinanceStatus,
  ReportStatus,
  ReportType,
} from './types';

export const reportTypeLabel: Record<ReportType, string> = {
  RevolvingFund: 'Replenishment of Revolving Fund',
  CashAdvanceLiquidation: 'Liquidation of Cash Advance',
  Reimbursement: 'Reimbursement',
};

export const reportTypeOptions = (Object.keys(reportTypeLabel) as ReportType[]).map((value) => ({
  value,
  title: reportTypeLabel[value],
}));

export const reportStatusLabel: Record<ReportStatus, string> = {
  New: 'New',
  ForApproval: 'For Approval',
  Returned: 'Returned',
  Approved: 'Approved',
  Rejected: 'Rejected',
};

export const reportStatusColor: Record<ReportStatus, string> = {
  New: 'secondary',
  ForApproval: 'warning',
  Returned: 'error',
  Approved: 'success',
  Rejected: 'error',
};

export const reportStatusOptions = (Object.keys(reportStatusLabel) as ReportStatus[]).map((value) => ({
  value,
  title: reportStatusLabel[value],
}));

export const approvalStatusColor: Record<ApprovalStatus, string> = {
  Pending: 'grey',
  Approved: 'success',
  Returned: 'error',
  Superseded: 'grey-lighten-1',
  Rejected: 'error',
  Overridden: 'grey-lighten-1',
};

export const approvalStatusLabel: Record<ApprovalStatus, string> = {
  Pending: 'Pending',
  Approved: 'Approved',
  Returned: 'Returned',
  Superseded: 'Superseded',
  Rejected: 'Rejected',
  Overridden: 'Overridden by Level 2',
};

export const financeStatusLabel: Record<FinanceStatus, string> = {
  Pending: 'Receipts not yet received',
  ReceiptsIncomplete: 'Receipts incomplete',
  ReceiptsReceived: 'Receipts received',
  ClearedForPayment: 'Cleared for payment',
};

/** Pill tone per Finance status (classes from main.css). */
export const financeStatusTone: Record<FinanceStatus, string> = {
  Pending: 'pill--neutral',
  ReceiptsIncomplete: 'pill--error',
  ReceiptsReceived: 'pill--warning',
  ClearedForPayment: 'pill--success',
};

export const categoryLabel: Record<ExpenseCategory, string> = {
  Meals: 'Meals',
  Transportation: 'Transportation',
  Toll: 'Toll',
  Parking: 'Parking',
  Others: 'Others',
  Representation: 'Representation',
  Sponsorship: 'Sponsorship',
  Demo: 'Demo',
  Lodging: 'Lodging',
  Gas: 'Gas',
  VehicleEmergency: 'Vehicle emergency purchase',
};

export const categoryOptions = (Object.keys(categoryLabel) as ExpenseCategory[]).map((value) => ({
  value,
  title: categoryLabel[value],
}));

export const expenseTypeOptions: { value: ExpenseType; title: string }[] = [
  { value: 'Local', title: 'Local travel' },
  { value: 'Foreign', title: 'Foreign (Oth column)' },
];

export const mealTypes = ['Breakfast', 'Lunch', 'Dinner', 'OT Meal'] as const;

/**
 * What a new expense line starts with for each category, so the employee only changes what differs.
 * Meals also preselects meal types (their configured rates become the amount); Transportation opens one
 * fare leg. Same for everyone; edit this table to change the defaults.
 */
export interface CategoryDefaults {
  particulars: string;
  meals: readonly (typeof mealTypes)[number][];
  fareLeg: boolean;
}

export const categoryDefaults: Record<ExpenseCategory, CategoryDefaults> = {
  Meals: { particulars: 'Meals', meals: ['Lunch'], fareLeg: false },
  Transportation: { particulars: 'Transportation', meals: [], fareLeg: true },
  Toll: { particulars: 'Toll', meals: [], fareLeg: false },
  Parking: { particulars: 'Parking', meals: [], fareLeg: false },
  Representation: { particulars: 'Representation', meals: [], fareLeg: false },
  Others: { particulars: '', meals: [], fareLeg: false },
  Sponsorship: { particulars: 'Sponsorship', meals: [], fareLeg: false },
  Demo: { particulars: 'Demo', meals: [], fareLeg: false },
  Lodging: { particulars: 'Lodging', meals: [], fareLeg: false },
  // Gas and Vehicle emergency purchase: Particulars come from the reason, vehicle model and plate no. fields.
  Gas: { particulars: '', meals: [], fareLeg: false },
  VehicleEmergency: { particulars: '', meals: [], fareLeg: false },
};

/** Only a Liquidation of Cash Advance carries the CASH ADVANCE RECEIVED block. */
export const usesCashAdvance = (type: ReportType) => type === 'CashAdvanceLiquidation';
/** Only a Replenishment of Revolving Fund carries the "IF WITH REVOLVING FUND" amount. */
export const usesRevolvingFund = (type: ReportType) => type === 'RevolvingFund';

/**
 * Who the closing balance (cash advance − expenses) is due to. Mirrors Domain BalanceDueRules:
 * a positive remainder is unspent advance owed to MDMPI; anything else is due to the employee.
 */
export function balanceDueTo(balance: number): 'MDMPI' | 'Employee' {
  return balance > 0 ? 'MDMPI' : 'Employee';
}

/** Banker's rounding (round half to even), matching VB's Math.Round used by the desktop ERF. */
function roundHalfEven(value: number, decimals: number): number {
  const factor = 10 ** decimals;
  const scaled = value * factor;
  const floor = Math.floor(scaled);
  const diff = scaled - floor;
  const epsilon = 1e-9;
  let rounded: number;
  if (Math.abs(diff - 0.5) < epsilon) {
    rounded = floor % 2 === 0 ? floor : floor + 1;
  } else {
    rounded = Math.round(scaled);
  }
  return rounded / factor;
}

/**
 * Preview of the server's line total (Domain ExpenseLineRules.Total): a receipt-backed line with
 * multiplier 1 keeps centavos; every other line is rounded to whole pesos, as the desktop did.
 */
export function lineTotal(amount: number, multiplier: number, hasInvoice: boolean): number {
  const raw = amount * Math.max(1, multiplier);
  return hasInvoice && multiplier === 1 ? roundHalfEven(raw, 2) : roundHalfEven(raw, 0);
}

/** The R47 column an amount lands in, for the lines table hint. */
export function r47Column(category: ExpenseCategory, type: ExpenseType): string {
  if (category === 'Representation') {
    return 'Representation';
  }
  if (type === 'Foreign') {
    return 'Oth';
  }
  // Sponsorship, Demo, Lodging, Gas and Vehicle emergency purchase, like Others, print under OTHERS.
  const columns: Partial<Record<ExpenseCategory, string>> = { Meals: 'MEALS', Transportation: 'TRANSP.', Toll: 'TOLL', Parking: 'PARKING' };
  return columns[category] ?? 'OTHERS';
}

/** Upload picker names, in the server's order. */
export const documentTypeOptions: { value: AttachmentDocumentType; title: string }[] = [
  { value: 'Receipt', title: 'Receipt' },
  { value: 'ActivityRequestForm', title: 'Activity Request Form' },
  { value: 'IsoAttendanceSheet', title: 'ISO attendance sheet' },
  { value: 'FoodReceipt', title: 'Food receipt' },
  { value: 'LodgingReceipt', title: 'Lodging receipt' },
  { value: 'GasReceipt', title: 'Gas receipt' },
  { value: 'VehiclePurchaseReceipt', title: 'Vehicle purchase receipt' },
  { value: 'JobOrder', title: 'Job Order (JO)' },
];
export const documentTypeLabel = Object.fromEntries(documentTypeOptions.map((o) => [o.value, o.title])) as Record<AttachmentDocumentType, string>;

/** The document an attachment counts as; an attendance sheet counts only once its form code was found or confirmed (server rule). */
/** Upload types that must show an ISO form code (attendance sheet R94, Job Order R42) to count. */
export const hasFormCode = (type: AttachmentDocumentType | null | undefined): boolean => type === 'IsoAttendanceSheet' || type === 'JobOrder';

/** Form code check of an attendance sheet or Job Order: Finance looks closer at the ones the employee only confirmed. */
export const formCodeText = { Found: 'Code found', Confirmed: 'Confirmed by employee', None: 'Not checked' } as const;
export const formCodeTitle = {
  Found: 'The form code was found in the PDF.',
  Confirmed: 'A photo or scan: the employee confirmed it is the form.',
  None: 'Uploaded before the form code check; upload it again so it counts.',
} as const;

export const countedDocumentType =(a: Attachment): AttachmentDocumentType =>
  hasFormCode(a.documentType) && !a.formCodeCheck ? 'Receipt' : (a.documentType ?? 'Receipt');

/** Icon for a fare mode by its code; unknown or admin-added codes get a generic route icon. */
const fareModeIcons: Record<string, string> = {
  TAXI: 'mdi-taxi',
  GRAB: 'mdi-car-side',
  ANGKAS: 'mdi-motorbike',
  JEEP: 'mdi-jeepney',
  TRIKE: 'mdi-rickshaw',
  PEDICAB: 'mdi-bicycle',
  BUS: 'mdi-bus',
  UV: 'mdi-van-passenger',
  RAIL: 'mdi-train',
  BOAT: 'mdi-ferry',
  HABAL: 'mdi-motorbike',
  OTHERS: 'mdi-dots-horizontal',
};

export function fareModeIcon(code: string | null | undefined): string {
  return (code && fareModeIcons[code]) || 'mdi-map-marker-path';
}

// Gas and Vehicle emergency purchase are for the issued vehicle: Particulars read "reason - vehicle model - plate no."
// (Domain VehicleParticulars). The reason may contain " - "; the last two parts are the model and the plate.
export const VEHICLE_SEPARATOR = ' - ';

export function isVehicleCategory(category: ExpenseCategory): boolean {
  return category === 'Gas' || category === 'VehicleEmergency';
}

export function vehicleParts(particulars: string): { reason: string; model: string; plate: string } {
  const parts = particulars.split(VEHICLE_SEPARATOR);
  if (parts.length < 3) return { reason: particulars.trim(), model: '', plate: '' };
  return {
    reason: parts.slice(0, -2).join(VEHICLE_SEPARATOR).trim(),
    model: parts[parts.length - 2].trim(),
    plate: parts[parts.length - 1].trim(),
  };
}

/** Suggested reasons for the Reason field; the employee may still type their own. */
export const vehicleReasons: Record<'Gas' | 'VehicleEmergency', string[]> = {
  Gas: ['Client visit', 'Service call', 'Preventive maintenance', 'Installation', 'Delivery', 'Demo', 'Training', 'Office errand'],
  VehicleEmergency: [
    'Flat tire',
    'Tire replacement',
    'Battery replacement',
    'Jump start',
    'Towing',
    'Engine overheating',
    'Brake repair',
    'Busted light',
    'Wiper replacement',
    'Oil or coolant top-up',
  ],
};

/** A plate number as the server saves it: upper-case, single spaces. */
export function normalizePlate(plate: string): string {
  return plate.trim().toUpperCase().split(/\s+/).filter(Boolean).join(' ');
}
