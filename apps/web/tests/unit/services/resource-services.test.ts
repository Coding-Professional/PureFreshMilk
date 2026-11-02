// @vitest-environment node
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getHousehold, setUpHousehold } from "@/services/household-service";
import {
  addMilkRate,
  deleteMilkRate,
  getMilkRateInEffect,
  listMilkRates,
} from "@/services/milk-rate-service";
import {
  addRecipient,
  archiveRecipient,
  listRecipients,
  unarchiveRecipient,
  updateRecipient,
} from "@/services/recipient-service";

const BASE = "http://localhost:5080/api/v1";

beforeEach(() => vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080"));

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

function stubFetch(response: Response) {
  const fetchMock = vi.fn().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

/** The method, URL and parsed JSON body of the one request that was made. */
function sent(fetchMock: ReturnType<typeof vi.fn>) {
  const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
  return {
    url,
    method: init.method,
    body: typeof init.body === "string" ? (JSON.parse(init.body) as unknown) : undefined,
  };
}

function noRateProblem() {
  return new Response(JSON.stringify({ status: 404, code: "no-rate-in-effect" }), {
    status: 404,
    headers: { "Content-Type": "application/problem+json" },
  });
}

describe("household service", () => {
  it("reads the household", async () => {
    const household = { id: "h", name: "Home", timeZoneId: "Asia/Kolkata", today: "2026-10-30" };
    const fetchMock = stubFetch(Response.json(household));

    await expect(getHousehold()).resolves.toEqual(household);
    expect(sent(fetchMock)).toMatchObject({ url: `${BASE}/household`, method: "GET" });
  });

  it("sets the household up with a PUT", async () => {
    const fetchMock = stubFetch(Response.json({ id: "h" }));

    await setUpHousehold({ name: "Home", timeZoneId: "Europe/London" });

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/household`,
      method: "PUT",
      body: { name: "Home", timeZoneId: "Europe/London" },
    });
  });
});

describe("recipient service", () => {
  it("lists the recipients from the items of the response", async () => {
    const recipient = {
      id: "r",
      name: "Recipient A",
      expectedMl: 1000,
      startsOn: "2026-10-01",
      archivedFrom: null,
    };
    const fetchMock = stubFetch(Response.json({ items: [recipient] }));

    await expect(listRecipients()).resolves.toEqual([recipient]);
    expect(sent(fetchMock).url).toBe(`${BASE}/recipients`);
  });

  it("asks for archived recipients only when told to", async () => {
    const fetchMock = stubFetch(Response.json({ items: [] }));

    await listRecipients({ includeArchived: true });

    expect(sent(fetchMock).url).toBe(`${BASE}/recipients?includeArchived=true`);
  });

  it("adds a recipient with a POST", async () => {
    const fetchMock = stubFetch(Response.json({ id: "r" }, { status: 201 }));

    await addRecipient({ name: "Recipient A", expectedMl: 1000, startsOn: "2026-10-01" });

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/recipients`,
      method: "POST",
      body: { name: "Recipient A", expectedMl: 1000, startsOn: "2026-10-01" },
    });
  });

  it("updates a recipient with a PUT to its address", async () => {
    const fetchMock = stubFetch(Response.json({ id: "r/1" }));

    await updateRecipient("r/1", { name: "Recipient B", expectedMl: 500, startsOn: "2026-10-01" });

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/recipients/r%2F1`,
      method: "PUT",
      body: { name: "Recipient B", expectedMl: 500, startsOn: "2026-10-01" },
    });
  });

  it("archives from today by sending no date", async () => {
    const fetchMock = stubFetch(Response.json({ id: "r" }));

    await archiveRecipient("r");

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/recipients/r/archive`,
      method: "POST",
      body: undefined,
    });
  });

  it("archives from a chosen date", async () => {
    const fetchMock = stubFetch(Response.json({ id: "r" }));

    await archiveRecipient("r", "2026-11-15");

    expect(sent(fetchMock).body).toEqual({ archivedFrom: "2026-11-15" });
  });

  it("restores an archived recipient", async () => {
    const fetchMock = stubFetch(Response.json({ id: "r" }));

    await unarchiveRecipient("r");

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/recipients/r/unarchive`,
      method: "POST",
      body: undefined,
    });
  });
});

describe("milk rate service", () => {
  it("lists the rates from the items of the response", async () => {
    const rate = { id: "m", effectiveFrom: "2026-10-01", pricePerLitrePaise: 6250 };
    const fetchMock = stubFetch(Response.json({ items: [rate] }));

    await expect(listMilkRates()).resolves.toEqual([rate]);
    expect(sent(fetchMock).url).toBe(`${BASE}/milk-rates`);
  });

  it("asks which rate is in force today, or on a date", async () => {
    const rate = { id: "m", effectiveFrom: "2026-10-01", pricePerLitrePaise: 6250 };
    const today = stubFetch(Response.json(rate));
    await expect(getMilkRateInEffect()).resolves.toEqual(rate);
    expect(sent(today).url).toBe(`${BASE}/milk-rates/in-effect`);

    const onDate = stubFetch(Response.json(rate));
    await getMilkRateInEffect("2026-10-14");
    expect(sent(onDate).url).toBe(`${BASE}/milk-rates/in-effect?on=2026-10-14`);
  });

  it("answers null, not an error, when no price has started yet", async () => {
    stubFetch(noRateProblem());

    await expect(getMilkRateInEffect()).resolves.toBeNull();
  });

  it("still fails for other errors", async () => {
    stubFetch(new Response(null, { status: 500 }));

    await expect(getMilkRateInEffect()).rejects.toMatchObject({ kind: "http", status: 500 });
  });

  it("adds a rate with a POST", async () => {
    const fetchMock = stubFetch(Response.json({ id: "m" }, { status: 201 }));

    await addMilkRate({ effectiveFrom: "2026-10-01", pricePerLitrePaise: 6250 });

    expect(sent(fetchMock)).toEqual({
      url: `${BASE}/milk-rates`,
      method: "POST",
      body: { effectiveFrom: "2026-10-01", pricePerLitrePaise: 6250 },
    });
  });

  it("deletes a rate", async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }));

    await expect(deleteMilkRate("m")).resolves.toBeUndefined();
    expect(sent(fetchMock)).toMatchObject({ url: `${BASE}/milk-rates/m`, method: "DELETE" });
  });
});
