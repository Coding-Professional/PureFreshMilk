import type { Recipient } from "@/types/household";

/**
 * SYNTHETIC DEMO DATA for the visual prototype.
 * These are not real people and are not loaded from the API or the database.
 * Anything built on this data must be labelled as demo data in the UI.
 */
export const DEMO_RECIPIENTS: readonly Recipient[] = [
  { id: "demo-a", name: "Recipient A", expectedMl: 1000 },
  { id: "demo-b", name: "Recipient B", expectedMl: 500 },
  { id: "demo-c", name: "Recipient C", expectedMl: 500 },
];
