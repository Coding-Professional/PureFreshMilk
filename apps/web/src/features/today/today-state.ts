import { recipientMarker } from "@/lib/recipient-marker";
import type { DeliveryEntry, DeliveryStatus, Recipient } from "@/types/household";

/**
 * Client-side state for the Today prototype.
 *
 * PROTOTYPE ONLY: these totals are display arithmetic over in-memory demo data.
 * Once deliveries are persisted, the API is the source of truth for quantities,
 * charges and balances; the UI will render what the server returns.
 */

export const QUANTITY_STEP_ML = 250;
export const MAX_DELIVERY_ML = 5000;

export interface TodayState {
  recipients: readonly Recipient[];
  entries: Readonly<Record<string, DeliveryEntry>>;
}

export type TodayAction =
  | { type: "deliver"; recipientId: string }
  | { type: "miss"; recipientId: string }
  | { type: "clear"; recipientId: string }
  | { type: "adjust"; recipientId: string; deltaMl: number }
  | { type: "deliverRemaining" }
  | { type: "reset" };

export function createTodayState(recipients: readonly Recipient[]): TodayState {
  return {
    recipients,
    entries: Object.fromEntries(
      recipients.map((recipient) => [recipient.id, pendingEntry(recipient.id)]),
    ),
  };
}

export function todayReducer(state: TodayState, action: TodayAction): TodayState {
  switch (action.type) {
    case "deliver":
      return updateEntry(state, action.recipientId, (entry, recipient) => ({
        ...entry,
        status: "delivered",
        deliveredMl: recipient.expectedMl,
      }));
    case "miss":
      return updateEntry(state, action.recipientId, (entry) => ({
        ...entry,
        status: "missed",
        deliveredMl: 0,
      }));
    case "clear":
      return updateEntry(state, action.recipientId, (entry) => pendingEntry(entry.recipientId));
    case "adjust":
      return updateEntry(state, action.recipientId, (entry) =>
        entry.status === "delivered"
          ? { ...entry, deliveredMl: clampDelivery(entry.deliveredMl + action.deltaMl) }
          : entry,
      );
    case "deliverRemaining":
      return {
        ...state,
        entries: Object.fromEntries(
          state.recipients.map((recipient) => {
            const entry = state.entries[recipient.id] ?? pendingEntry(recipient.id);
            return [
              recipient.id,
              entry.status === "pending"
                ? { ...entry, status: "delivered", deliveredMl: recipient.expectedMl }
                : entry,
            ];
          }),
        ),
      };
    case "reset":
      return createTodayState(state.recipients);
  }
}

export interface DaySummary {
  expectedMl: number;
  deliveredMl: number;
  counts: Record<DeliveryStatus, number>;
}

export function summarizeDay(state: TodayState): DaySummary {
  const summary: DaySummary = {
    expectedMl: 0,
    deliveredMl: 0,
    counts: { pending: 0, delivered: 0, missed: 0 },
  };

  for (const recipient of state.recipients) {
    const entry = state.entries[recipient.id] ?? pendingEntry(recipient.id);
    summary.expectedMl += recipient.expectedMl;
    summary.deliveredMl += entry.deliveredMl;
    summary.counts[entry.status] += 1;
  }

  return summary;
}

/** One row of the Today list: the recipient, their marker letter and today's entry. */
export interface TodayRow {
  recipient: Recipient;
  marker: string;
  entry: DeliveryEntry;
  /** Delivered minus usual quantity; zero unless delivered. */
  differenceMl: number;
}

export function todayRows(state: TodayState): TodayRow[] {
  return state.recipients.map((recipient) => {
    const entry = state.entries[recipient.id] ?? pendingEntry(recipient.id);
    return {
      recipient,
      marker: recipientMarker(recipient.name),
      entry,
      differenceMl: entry.status === "delivered" ? entry.deliveredMl - recipient.expectedMl : 0,
    };
  });
}

function pendingEntry(recipientId: string): DeliveryEntry {
  return { recipientId, status: "pending", deliveredMl: 0 };
}

function clampDelivery(millilitres: number): number {
  return Math.min(MAX_DELIVERY_ML, Math.max(QUANTITY_STEP_ML, millilitres));
}

function updateEntry(
  state: TodayState,
  recipientId: string,
  update: (entry: DeliveryEntry, recipient: Recipient) => DeliveryEntry,
): TodayState {
  const recipient = state.recipients.find((candidate) => candidate.id === recipientId);
  if (!recipient) return state;

  const entry = state.entries[recipientId] ?? pendingEntry(recipientId);
  return { ...state, entries: { ...state.entries, [recipientId]: update(entry, recipient) } };
}
