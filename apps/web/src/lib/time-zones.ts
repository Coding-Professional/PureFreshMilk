/**
 * Time zones offered when setting up a household. The API accepts any IANA id; this short list covers the
 * likely cases, and a household already on another zone keeps it (see timeZoneOptions).
 */
export const DEFAULT_TIME_ZONE_ID = "Asia/Kolkata";

const COMMON_TIME_ZONES: readonly { id: string; label: string }[] = [
  { id: "Asia/Kolkata", label: "India (Asia/Kolkata)" },
  { id: "Asia/Dubai", label: "United Arab Emirates (Asia/Dubai)" },
  { id: "Asia/Singapore", label: "Singapore (Asia/Singapore)" },
  { id: "Europe/London", label: "United Kingdom (Europe/London)" },
  { id: "America/New_York", label: "US Eastern (America/New_York)" },
  { id: "America/Los_Angeles", label: "US Pacific (America/Los_Angeles)" },
  { id: "Australia/Sydney", label: "Sydney (Australia/Sydney)" },
  { id: "UTC", label: "UTC" },
];

/** The common zones, plus `currentId` when the household is on a zone that is not in the list. */
export function timeZoneOptions(currentId?: string): readonly { id: string; label: string }[] {
  if (!currentId || COMMON_TIME_ZONES.some((zone) => zone.id === currentId)) {
    return COMMON_TIME_ZONES;
  }
  return [{ id: currentId, label: currentId }, ...COMMON_TIME_ZONES];
}
