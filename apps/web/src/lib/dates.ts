/** "Thursday, 29 October" in the viewer's locale (or the given one). */
export function formatLongDate(date: Date, locale?: string): string {
  return new Intl.DateTimeFormat(locale, {
    weekday: "long",
    day: "numeric",
    month: "long",
  }).format(date);
}
