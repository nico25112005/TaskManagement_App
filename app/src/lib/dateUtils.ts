/**
 * Returns a local date string in YYYY-MM-DD format, avoiding timezone offset issues.
 * toISOString() converts to UTC which can shift the date by a day.
 */
export function toLocalISODate(date: Date): string {
  const year = date.getFullYear();
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const day = date.getDate().toString().padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Returns today's date as a local YYYY-MM-DD string.
 */
export function todayLocal(): string {
  return toLocalISODate(new Date());
}