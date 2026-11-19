// Job grades from Master data -> Job grades: the code is stored, the name is shown.

export interface JobGradeOption {
  code: string;
  name: string;
  isActive: boolean;
}

/** Dropdown items: the active grades, plus `current` when its grade was deactivated since, so the record keeps it. */
export function jobGradeItems(grades: JobGradeOption[], current?: string | null): { value: string; title: string }[] {
  return grades
    .filter((g) => g.isActive || g.code === current)
    .map((g) => ({ value: g.code, title: g.isActive ? g.name : `${g.name} (inactive)` }));
}

/** A code -> its name; a code not in the list (an older free-text grade) shows as typed. */
export function jobGradeName(grades: JobGradeOption[], code: string | null | undefined): string {
  if (!code) return '';
  return grades.find((g) => g.code === code)?.name ?? code;
}
