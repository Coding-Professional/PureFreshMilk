// @vitest-environment node
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  clearDelivery,
  deliverRemaining,
  getDaySheet,
  listDaySummaries,
  recordDelivery,
} from "@/services/delivery-service";

beforeEach(() => vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080"));
afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe("delivery service", () => {
  it("uses the day sheet and date range endpoints", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ items: [] })));
    vi.stubGlobal("fetch", fetchMock);
    await getDaySheet("today");
    await listDaySummaries("2026-10-01", "2026-10-30");
    expect(fetchMock.mock.calls[0]![0]).toBe("http://localhost:5080/api/v1/day-sheets/today");
    expect(fetchMock.mock.calls[1]![0]).toBe(
      "http://localhost:5080/api/v1/day-summaries?from=2026-10-01&to=2026-10-30",
    );
  });

  it("sends idempotent writes and the batch action", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(Response.json({}))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(Response.json({}));
    vi.stubGlobal("fetch", fetchMock);
    await recordDelivery("r-1", "2026-10-30", "delivered", 1250);
    await clearDelivery("r-1", "2026-10-30");
    await deliverRemaining("2026-10-30");
    expect(fetchMock.mock.calls.map(([url, init]) => [url, init.method])).toEqual([
      ["http://localhost:5080/api/v1/recipients/r-1/deliveries/2026-10-30", "PUT"],
      ["http://localhost:5080/api/v1/recipients/r-1/deliveries/2026-10-30", "DELETE"],
      ["http://localhost:5080/api/v1/day-sheets/2026-10-30/deliver-remaining", "POST"],
    ]);
    expect(JSON.parse(fetchMock.mock.calls[0]![1].body as string)).toEqual({
      status: "delivered",
      deliveredMl: 1250,
    });
  });
});
