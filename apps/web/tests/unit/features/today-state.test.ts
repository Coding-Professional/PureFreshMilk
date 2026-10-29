import { describe, expect, it } from "vitest";
import {
  createTodayState,
  MAX_DELIVERY_ML,
  QUANTITY_STEP_ML,
  summarizeDay,
  todayReducer,
  todayRows,
  type TodayAction,
} from "@/features/today/today-state";
import type { Recipient } from "@/types/household";

const recipients: Recipient[] = [
  { id: "a", name: "Recipient A", expectedMl: 1000 },
  { id: "b", name: "Recipient B", expectedMl: 500 },
  { id: "c", name: "Recipient C", expectedMl: 500 },
];

const apply = (...actions: TodayAction[]) =>
  actions.reduce(todayReducer, createTodayState(recipients));

describe("today state", () => {
  it("starts with everyone unmarked and 2 L expected", () => {
    const summary = summarizeDay(createTodayState(recipients));

    expect(summary).toEqual({
      expectedMl: 2000,
      deliveredMl: 0,
      counts: { pending: 3, delivered: 0, missed: 0 },
    });
  });

  it("delivering records the recipient's usual quantity", () => {
    const state = apply({ type: "deliver", recipientId: "a" });

    expect(state.entries.a).toEqual({ recipientId: "a", status: "delivered", deliveredMl: 1000 });
    expect(summarizeDay(state).deliveredMl).toBe(1000);
  });

  it("missing records zero and clearing returns to unmarked", () => {
    const missed = apply({ type: "deliver", recipientId: "b" }, { type: "miss", recipientId: "b" });
    expect(missed.entries.b).toMatchObject({ status: "missed", deliveredMl: 0 });

    const cleared = todayReducer(missed, { type: "clear", recipientId: "b" });
    expect(cleared.entries.b).toMatchObject({ status: "pending", deliveredMl: 0 });
  });

  it("adjusting changes only delivered entries and stays within limits", () => {
    const unmarked = apply({ type: "adjust", recipientId: "a", deltaMl: QUANTITY_STEP_ML });
    expect(unmarked.entries.a?.deliveredMl).toBe(0);

    const tooLow = apply(
      { type: "deliver", recipientId: "b" },
      { type: "adjust", recipientId: "b", deltaMl: -10_000 },
    );
    expect(tooLow.entries.b?.deliveredMl).toBe(QUANTITY_STEP_ML);

    const tooHigh = apply(
      { type: "deliver", recipientId: "b" },
      { type: "adjust", recipientId: "b", deltaMl: 10_000 },
    );
    expect(tooHigh.entries.b?.deliveredMl).toBe(MAX_DELIVERY_ML);
  });

  it("marking the rest delivered leaves missed entries alone", () => {
    const state = apply({ type: "miss", recipientId: "c" }, { type: "deliverRemaining" });

    expect(summarizeDay(state)).toEqual({
      expectedMl: 2000,
      deliveredMl: 1500,
      counts: { pending: 0, delivered: 2, missed: 1 },
    });
  });

  it("reset starts the day over", () => {
    const state = todayReducer(apply({ type: "deliverRemaining" }), { type: "reset" });

    expect(summarizeDay(state).counts.pending).toBe(3);
  });

  it("ignores unknown recipients", () => {
    const before = createTodayState(recipients);

    expect(todayReducer(before, { type: "deliver", recipientId: "nobody" })).toBe(before);
  });

  it("rows report markers and the difference from the usual quantity", () => {
    const state = apply(
      { type: "deliver", recipientId: "a" },
      { type: "adjust", recipientId: "a", deltaMl: 500 },
    );
    const [first, second] = todayRows(state);

    expect(first).toMatchObject({ marker: "A", differenceMl: 500 });
    expect(second).toMatchObject({ marker: "B", differenceMl: 0 });
  });
});
