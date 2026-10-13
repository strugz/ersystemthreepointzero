import { categoryLabel } from '@/features/expenses/labels';
import type { MealSubCategory, RateCategory, RateLine, RateTargetType, RateType, ScopeAssignmentType, ScopeLevel } from './types';

export { categoryLabel, categoryOptions } from '@/features/expenses/labels';

export const scopeTypeLabel: Record<ScopeAssignmentType, string> = {
  Department: 'Whole department',
  Individual: 'Named people',
  Exclusion: 'Department except named people',
};

export const scopeTypeHint: Record<ScopeAssignmentType, string> = {
  Department: 'Signs for everyone in the department.',
  Individual: 'Signs only for the people named. No department.',
  Exclusion: 'Signs for the department minus the people named.',
};

export const scopeTypeOptions = (Object.keys(scopeTypeLabel) as ScopeAssignmentType[]).map((value) => ({
  value,
  title: scopeTypeLabel[value],
}));

export const scopeLevelLabel: Record<ScopeLevel, string> = { 1: 'Level 1', 2: 'Level 2' };
export const scopeLevelOptions: { value: ScopeLevel; title: string }[] = [
  { value: 1, title: 'Level 1' },
  { value: 2, title: 'Level 2' },
];

export const rateTypeLabel: Record<RateType, string> = {
  FixedPerDay: 'Fixed per day',
  PerKm: 'Per km',
  CapPerTransaction: 'Cap per transaction',
  ActualWithCeiling: 'Actual, with ceiling',
};
export const rateTypeOptions = (Object.keys(rateTypeLabel) as RateType[]).map((value) => ({ value, title: rateTypeLabel[value] }));

export const targetTypeLabel: Record<RateTargetType, string> = {
  Individual: 'Individual',
  Grade: 'Job grade',
  Department: 'Department',
  Company: 'Company-wide',
};
export const targetTypeOptions = (Object.keys(targetTypeLabel) as RateTargetType[]).map((value) => ({
  value,
  title: targetTypeLabel[value],
}));

export const mealLabel: Record<MealSubCategory, string> = {
  Breakfast: 'Breakfast',
  Lunch: 'Lunch',
  Dinner: 'Dinner',
  OTMeal: 'OT Meal',
};
export const mealOptions = (Object.keys(mealLabel) as MealSubCategory[]).map((value) => ({ value, title: mealLabel[value] }));

/** "Meals · Lunch" / "Transportation". */
export function rateKeyLabel(category: RateCategory, subCategory: MealSubCategory | null): string {
  const base = categoryLabel[category] ?? category;
  return subCategory ? `${base} · ${mealLabel[subCategory] ?? subCategory}` : base;
}

/** Stable key for grouping line versions (category + meal). */
export function rateKey(line: Pick<RateLine, 'category' | 'subCategory'>): string {
  return `${line.category}|${line.subCategory ?? ''}`;
}

/** Who a rate assignment applies to, in words. */
export function rateTargetText(a: {
  targetType: RateTargetType;
  userName: string | null;
  jobGrade: string | null;
  departmentName: string | null;
}): string {
  switch (a.targetType) {
    case 'Individual':
      return a.userName ?? '—';
    case 'Grade':
      return a.jobGrade ? `Grade ${a.jobGrade}` : '—';
    case 'Department':
      return a.departmentName ?? '—';
    default:
      return 'Everyone';
  }
}
