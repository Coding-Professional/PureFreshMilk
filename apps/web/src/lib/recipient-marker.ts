/**
 * Short label shown in a recipient's marker and in the day's measure.
 * "Recipient A" → "A", "Asha" → "A", "Asha Rao" → "AR".
 */
export function recipientMarker(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const first = words[0];
  const last = words.at(-1);

  if (!first || !last) return "?";
  if (words.length === 1 || last.length === 1) return last.charAt(0).toUpperCase();
  return `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
}
