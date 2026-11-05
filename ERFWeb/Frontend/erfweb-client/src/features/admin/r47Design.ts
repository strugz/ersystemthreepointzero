// Geometry and limits of the R47 designer. They mirror the server (Domain R47Design / R47DesignRules),
// which stays the authority: these only drive the drawing and early hints, and the API validates again.
import type { R47Design } from './types';

export const PAGE_WIDTH = 792;
export const PAGE_HEIGHT = 612;
const CASH_ADVANCE_TITLE = 12;
const CASH_ADVANCE_HEADING = 12.5;
const CASH_ADVANCE_GAP = 1;
const REFERENCE_LINE = 11;
const MIN_COLUMN = 20;

export const GRID_COLUMN_NAMES = [
  'DATE',
  'PARTICULARS',
  'Invoice/OR',
  'MEALS',
  'TRANSP.',
  'TOLL',
  'PARKING',
  'OTHERS',
  'TOTAL',
  'Representation',
  'Other Exp.',
  'VAT',
  'GRAND TOTAL',
] as const;

/** A section of the form; clicking a band in the drawing opens its section in the panel. */
export type R47Section = 'page' | 'header' | 'employee' | 'grid' | 'totals' | 'cashAdvance' | 'footer' | 'type';

export type R47SizeKey = {
  [K in keyof R47Design]: R47Design[K] extends number ? K : never;
}[keyof R47Design];

/** Allowed range of each size (pt), as the server's R47DesignRules.Ranges. */
export const RANGES: Record<R47SizeKey, [number, number]> = {
  marginLeft: [0, 60],
  marginRight: [0, 60],
  marginTop: [0, 40],
  headerHeight: [50, 150],
  logoWidth: [40, 250],
  documentCodeWidth: [60, 200],
  pageNoWidth: [60, 200],
  companyNameSize: [8, 24],
  titleSize: [10, 28],
  infoLabelSize: [6, 12],
  infoValueSize: [8, 18],
  dateStampHeight: [8, 20],
  employeeLineHeight: [9, 24],
  employeeLabelWidth: [60, 200],
  gridHeadingHeight: [8, 24],
  gridSubHeadingHeight: [8, 24],
  singleRowHeight: [10, 24],
  detailHeight: [60, 400],
  totalsRowHeight: [10, 30],
  grandTotalRowHeight: [10, 30],
  cashAdvanceRowHeight: [9, 20],
  reconLineHeight: [9, 24],
  footerHeight: [90, 150],
  bodySize: [5, 10],
  smallSize: [4.5, 9],
  gridTextSize: [4.5, 8],
  amountSize: [5, 9],
  cashAdvanceWidth: [250, 450],
  footerLeftWidth: [200, 450],
};

export interface SizeField {
  key: R47SizeKey;
  label: string;
}

/** The size fields of each section, in panel order. */
export const SECTION_FIELDS: Record<R47Section, SizeField[]> = {
  page: [
    { key: 'marginLeft', label: 'Left margin' },
    { key: 'marginRight', label: 'Right margin' },
    { key: 'marginTop', label: 'Top margin' },
  ],
  header: [
    { key: 'headerHeight', label: 'Header height' },
    { key: 'logoWidth', label: 'Logo box width' },
    { key: 'documentCodeWidth', label: 'Document Code box width' },
    { key: 'pageNoWidth', label: 'Page No. box width' },
    { key: 'companyNameSize', label: 'Company name type size' },
    { key: 'titleSize', label: 'Title type size' },
    { key: 'infoLabelSize', label: 'Box label type size' },
    { key: 'infoValueSize', label: 'Box value type size' },
  ],
  employee: [
    { key: 'dateStampHeight', label: 'DATE stamp height' },
    { key: 'employeeLineHeight', label: 'Employee line height' },
    { key: 'employeeLabelWidth', label: 'Label width' },
  ],
  grid: [
    { key: 'gridHeadingHeight', label: 'Heading height' },
    { key: 'gridSubHeadingHeight', label: 'Sub-heading height' },
    { key: 'singleRowHeight', label: 'One-line row height' },
    { key: 'detailHeight', label: 'Detail area height (rows per page)' },
  ],
  totals: [
    { key: 'totalsRowHeight', label: 'Page totals row height' },
    { key: 'grandTotalRowHeight', label: 'GRAND TOTAL row height' },
  ],
  cashAdvance: [
    { key: 'cashAdvanceWidth', label: 'Cash advance box width' },
    { key: 'cashAdvanceRowHeight', label: 'Cash advance row height' },
    { key: 'reconLineHeight', label: 'Reconciliation line height' },
  ],
  footer: [
    { key: 'footerHeight', label: 'Footer height' },
    { key: 'footerLeftWidth', label: 'Promise box width' },
  ],
  type: [
    { key: 'bodySize', label: 'Body type size' },
    { key: 'smallSize', label: 'Small type size' },
    { key: 'gridTextSize', label: 'Grid text size (Tahoma)' },
    { key: 'amountSize', label: 'Amount type size' },
  ],
};

export const SECTION_TITLES: Record<R47Section, string> = {
  page: 'Page margins',
  header: 'Header',
  employee: 'DATE stamp and employee',
  grid: 'Grid',
  totals: 'Totals rows',
  cashAdvance: 'Cash advance and reconciliation',
  footer: 'Footer',
  type: 'Type and colours',
};

export const contentWidth = (d: R47Design) => PAGE_WIDTH - d.marginLeft - d.marginRight;
export const dateBoxWidth = (d: R47Design) => d.gridColumns.slice(-4).reduce((a, b) => a + b, 0);
export const titleWidth = (d: R47Design) => contentWidth(d) - d.logoWidth - d.documentCodeWidth - d.pageNoWidth;
export const promiseLines = (d: R47Design) =>
  d.promiseText
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.length > 0);
export const cashAdvanceHeight = (d: R47Design, rows = 1) =>
  Math.max(CASH_ADVANCE_TITLE + CASH_ADVANCE_HEADING + (Math.max(rows, 1) + 1) * d.cashAdvanceRowHeight, 3 * d.reconLineHeight);

/** Top of every band (pt from the page top), as the server's R47Composer stacks them. */
export interface R47Bands {
  header: number;
  dateStamp: number;
  employee: number;
  heading: number;
  subHeading: number;
  detail: number;
  totals: number;
  grandTotal: number;
  cashAdvance: number;
  footer: number;
  reference: number;
  end: number;
}

export function bands(d: R47Design): R47Bands {
  const header = d.marginTop;
  const dateStamp = header + d.headerHeight;
  const employee = dateStamp + d.dateStampHeight;
  const heading = employee + 2 * d.employeeLineHeight;
  const subHeading = heading + d.gridHeadingHeight;
  const detail = subHeading + d.gridSubHeadingHeight;
  const totals = detail + d.detailHeight;
  const grandTotal = totals + d.totalsRowHeight;
  const cashAdvance = grandTotal + d.grandTotalRowHeight + CASH_ADVANCE_GAP;
  const footer = cashAdvance + cashAdvanceHeight(d);
  const reference = footer + d.footerHeight;
  return { header, dateStamp, employee, heading, subHeading, detail, totals, grandTotal, cashAdvance, footer, reference, end: reference + REFERENCE_LINE };
}

/** Left edge of each grid column, plus the right edge of the last. */
export function columnEdges(d: R47Design): number[] {
  const x = [d.marginLeft];
  for (const w of d.gridColumns) x.push(x[x.length - 1] + w);
  return x;
}

export const round = (v: number) => Math.round(v * 2) / 2;
export const clamp = (v: number, [min, max]: [number, number]) => Math.min(max, Math.max(min, v));

/** Moves the divider after grid column i by delta: column i grows, the next shrinks, the total stays. */
export function moveColumnDivider(d: R47Design, i: number, delta: number): number[] {
  const cols = [...d.gridColumns];
  const step = Math.max(MIN_COLUMN - cols[i], Math.min(cols[i + 1] - MIN_COLUMN, delta));
  cols[i] = round(cols[i] + step);
  cols[i + 1] = round(cols[i + 1] - step);
  return cols;
}

/**
 * Sets a size. A margin change is taken up by the PARTICULARS column so the grid keeps filling the
 * width between the margins.
 */
export function withSize(d: R47Design, key: R47SizeKey, value: number): R47Design {
  const next = { ...d, [key]: value };
  if (key === 'marginLeft' || key === 'marginRight') {
    const cols = [...d.gridColumns];
    cols[1] = round(cols[1] - (value - d[key]));
    next.gridColumns = cols;
  }
  return next;
}

/** Early hints for the most common mistakes; the server repeats every check on save and preview. */
export function designProblems(d: R47Design): string[] {
  const problems: string[] = [];
  for (const [key, [min, max]] of Object.entries(RANGES) as [R47SizeKey, [number, number]][]) {
    const v = d[key];
    if (!Number.isFinite(v) || v < min || v > max) problems.push(`${label(key)}: enter ${min} to ${max} pt.`);
  }
  if (d.gridColumns.some((w) => !Number.isFinite(w) || w < MIN_COLUMN)) {
    problems.push(`Each grid column needs at least ${MIN_COLUMN} pt.`);
  } else if (d.gridColumns[1] < 80 || d.gridColumns[2] < 30) {
    problems.push('PARTICULARS needs at least 80 pt and Invoice/OR at least 30 pt.');
  }
  const sum = d.gridColumns.reduce((a, b) => a + b, 0);
  if (Math.abs(sum - contentWidth(d)) > 0.5) {
    problems.push(`The grid columns add up to ${fmt(sum)} pt; they must fill the ${fmt(contentWidth(d))} pt between the margins.`);
  }
  if (titleWidth(d) < 150) problems.push('The company name and title need at least 150 pt in the header.');
  if (d.cashAdvanceWidth > contentWidth(d) - 330) problems.push('The reconciliation beside the cash advance box needs at least 330 pt.');
  if (d.footerLeftWidth > contentWidth(d) - 300) problems.push('The signatories beside the promise need at least 300 pt.');
  const promiseNeeds = 5 + promiseLines(d).length * d.bodySize * 1.45 + 31;
  if (promiseNeeds > d.footerHeight) problems.push(`The promise and the employee's signature need ${fmt(promiseNeeds)} pt of footer.`);
  const height = bands(d).end;
  if (height > PAGE_HEIGHT) problems.push(`The form is ${fmt(height)} pt tall and the page ${PAGE_HEIGHT} pt; shorten a band.`);
  return problems;
}

const fmt = (v: number) => (Math.round(v * 100) / 100).toString();

function label(key: R47SizeKey): string {
  for (const fields of Object.values(SECTION_FIELDS)) {
    const f = fields.find((x) => x.key === key);
    if (f) return f.label;
  }
  return key;
}
