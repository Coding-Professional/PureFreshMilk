/** "Thursday, 29 October" in the viewer's locale (or the given one). */
export function formatLongDate(date: Date, locale?: string): string {
  return new Intl.DateTimeFormat(locale, {
    weekday: "long",
    day: "numeric",
    month: "long",
  }).format(date);
}

/*
 * Calendar dates travel as "YYYY-MM-DD" strings and are never turned into a Date with the local time
 * zone: new Date("2026-10-30") is midnight UTC, which is the previous day in some zones. The household's
 * calendar date is decided by the server, so these helpers only read and display it.
 */

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;

/** Whether `value` is a real calendar date written as YYYY-MM-DD. */
export function isIsoDate(value: string): boolean {
  return toUtcDate(value) !== null;
}

/** "2026-10-30" → "30 Oct 2026" in the viewer's locale (or the given one). Anything else is returned as it is. */
export function formatIsoDate(value: string, locale?: string): string {
  const date = toUtcDate(value);
  if (!date) return value;

  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

function toUtcDate(value: string): Date | null {
  const match = ISO_DATE.exec(value);
  if (!match) return null;

  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const date = new Date(Date.UTC(year, month - 1, day));

  const isRealDate =
    date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
  return isRealDate ? date : null;
}
