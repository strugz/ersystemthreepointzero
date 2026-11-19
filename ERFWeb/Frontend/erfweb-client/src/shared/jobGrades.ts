// Job grades (Domain JobGrades): the code is stored, the label is shown.

export const jobGradeOptions = [
  { value: 'Staff', title: 'Staff' },
  { value: 'Specialist', title: 'Specialists/Engineers' },
  { value: 'Supervisor', title: 'Team Leaders/Supervisors/Assistant Managers' },
] as const;

/** A code -> its label; an older free-text grade shows as typed. */
export function jobGradeLabel(value: string | null | undefined): string {
  if (!value) return '';
  return jobGradeOptions.find((o) => o.value === value)?.title ?? value;
}

export function isJobGrade(value: string | null | undefined): boolean {
  return jobGradeOptions.some((o) => o.value === value);
}
