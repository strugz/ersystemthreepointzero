import type { ExpenseLine } from './types';

// The R47 prints lines in their saved order and starts a group header whenever the date, location
// or Work With changes. These helpers group the same way and build the orders the editor can save.

export interface LineGroup {
  key: string;
  date: string;
  location: string | null;
  workWith: string | null;
  lines: ExpenseLine[];
  total: number;
}

function groupKey(line: ExpenseLine): string {
  return `${line.transactionDate}|${line.location ?? ''}|${line.workWith ?? ''}`;
}

/** Consecutive lines with the same date, location and Work With, as the R47 groups them. */
export function groupLines(lines: readonly ExpenseLine[]): LineGroup[] {
  const groups: LineGroup[] = [];
  let lastKey: string | null = null;
  for (const line of lines) {
    const key = groupKey(line);
    let g = groups[groups.length - 1];
    if (!g || key !== lastKey) {
      // The same date and Work With can appear again further down, so the render key carries the position.
      g = { key: `${key}#${groups.length}`, date: line.transactionDate, location: line.location, workWith: line.workWith, lines: [], total: 0 };
      groups.push(g);
      lastKey = key;
    }
    g.lines.push(line);
    g.total += line.totalAmount;
  }
  return groups;
}

export type LineSort = 'dateAsc' | 'dateDesc' | 'totalDesc' | 'totalAsc';

export const lineSortOptions: { value: LineSort; label: string }[] = [
  { value: 'dateAsc', label: 'Date: oldest first' },
  { value: 'dateDesc', label: 'Date: newest first' },
  { value: 'totalDesc', label: 'Total: highest first' },
  { value: 'totalAsc', label: 'Total: lowest first' },
];

/**
 * Line ids in the new order. Lines of the same date, location and Work With are kept together so
 * each prints under one header; ties keep the current order (Array.sort is stable).
 * By total, the groups are ordered by their subtotal and the lines inside each group by amount.
 */
export function sortedLineIds(lines: readonly ExpenseLine[], sort: LineSort): number[] {
  const sign = sort === 'dateAsc' || sort === 'totalAsc' ? 1 : -1;
  // Cluster first (by date, then by first appearance of each location/Work With on that date).
  const byKey = new Map<string, ExpenseLine[]>();
  for (const line of [...lines].sort((a, b) => a.transactionDate.localeCompare(b.transactionDate))) {
    const key = groupKey(line);
    const list = byKey.get(key);
    if (list) list.push(line);
    else byKey.set(key, [line]);
  }
  const groups = groupLines([...byKey.values()].flat());

  if (sort === 'dateAsc' || sort === 'dateDesc') {
    groups.sort((a, b) => sign * a.date.localeCompare(b.date));
    return groups.flatMap((g) => g.lines.map((l) => l.id));
  }
  groups.sort((a, b) => sign * (a.total - b.total));
  return groups.flatMap((g) => [...g.lines].sort((a, b) => sign * (a.totalAmount - b.totalAmount)).map((l) => l.id));
}
