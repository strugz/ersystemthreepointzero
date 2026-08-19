export const required = (label: string) => (value: unknown) => String(value ?? '').trim().length > 0 || `${label} is required.`
export const maximumLength = (label: string, maximum: number) => (value: unknown) => String(value ?? '').length <= maximum || `${label} must be ${maximum} characters or fewer.`
