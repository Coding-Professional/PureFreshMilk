import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ConnectedDayScreen } from "@/features/today/connected-day-screen";

const { getDaySheet, recordDelivery, clearDelivery, deliverRemaining } = vi.hoisted(() => ({
  getDaySheet: vi.fn(),
  recordDelivery: vi.fn(),
  clearDelivery: vi.fn(),
  deliverRemaining: vi.fn(),
}));
vi.mock("@/services/delivery-service", () => ({
  getDaySheet: (...args: unknown[]) => getDaySheet(...args),
  recordDelivery: (...args: unknown[]) => recordDelivery(...args),
  clearDelivery: (...args: unknown[]) => clearDelivery(...args),
  deliverRemaining: (...args: unknown[]) => deliverRemaining(...args),
}));

const sheet = {
  date: "2026-10-30",
  isToday: true,
  rows: [
    {
      recipientId: "r-1",
      name: "Recipient A",
      expectedMl: 1000,
      status: "notRecorded",
      deliveredMl: 0,
    },
  ],
  totals: { expectedMl: 1000, deliveredMl: 0, delivered: 0, missed: 0, notRecorded: 1 },
};

beforeEach(() => {
  getDaySheet.mockReset().mockResolvedValue(sheet);
  recordDelivery.mockReset().mockResolvedValue({});
  clearDelivery.mockReset().mockResolvedValue(undefined);
  deliverRemaining.mockReset().mockResolvedValue(sheet);
});

describe("connected day screen", () => {
  it("records a delivery and reloads the server sheet", async () => {
    const user = userEvent.setup();
    render(<ConnectedDayScreen date="today" />);
    const button = await screen.findByRole("button", { name: "Delivered for Recipient A" });
    await user.click(button);
    await waitFor(() =>
      expect(recordDelivery).toHaveBeenCalledWith("r-1", "2026-10-30", "delivered", undefined),
    );
    await waitFor(() => expect(getDaySheet).toHaveBeenCalledTimes(2));
  });

  it("restores the previous row and announces a failed save", async () => {
    recordDelivery.mockRejectedValue(new Error("network"));
    const user = userEvent.setup();
    render(<ConnectedDayScreen date="today" />);
    const button = await screen.findByRole("button", { name: "Delivered for Recipient A" });
    await user.click(button);
    await screen.findByRole("alert");
    expect(button).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByRole("button", { name: "Reload day" })).toBeInTheDocument();
  });
});
