// @vitest-environment node
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  ApiError,
  apiDelete,
  apiGet,
  apiPost,
  apiPut,
  fieldErrors,
  getApiBaseUrl,
  hasProblemCode,
} from "@/services/api-client";
import { getApiStatus } from "@/services/status-service";

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

function stubFetch(response: Response | Error) {
  const fetchMock =
    response instanceof Error
      ? vi.fn().mockRejectedValue(response)
      : vi.fn().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function problemResponse(status: number, body: object) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

describe("getApiBaseUrl", () => {
  it("drops trailing slashes", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080//");
    expect(getApiBaseUrl()).toBe("http://localhost:5080");
  });

  it("is null when not configured", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "  ");
    expect(getApiBaseUrl()).toBeNull();
  });
});

describe("apiGet", () => {
  it("fails clearly when the API address is not configured", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "");

    await expect(apiGet("/status")).rejects.toMatchObject({ kind: "not-configured" });
  });

  it("requests JSON under /api/v1 and returns the body", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    const body = {
      service: "purefreshmilk-api",
      version: "0.1.0",
      serverTime: "2026-01-15T06:30:00Z",
    };
    const fetchMock = stubFetch(Response.json(body));

    await expect(getApiStatus()).resolves.toEqual(body);
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5080/api/v1/status",
      expect.objectContaining({ method: "GET", headers: { Accept: "application/json" } }),
    );
  });

  it("reports HTTP errors with their status", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(new Response(null, { status: 503 }));

    const error = await apiGet("/status").catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ kind: "http", status: 503 });
    expect((error as ApiError).problem).toBeUndefined();
  });

  it("reports network failures as unreachable", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(new TypeError("fetch failed"));

    await expect(apiGet("/status")).rejects.toMatchObject({ kind: "unreachable" });
  });
});

describe("writing", () => {
  it("sends a JSON body with POST and PUT", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    // A new Response per call: a body can only be read once.
    const fetchMock = vi.fn(async () => Response.json({ id: "1" }));
    vi.stubGlobal("fetch", fetchMock);

    await apiPost("/recipients", { name: "Recipient A" });
    await apiPut("/household", { name: "Home" });

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "http://localhost:5080/api/v1/recipients",
      expect.objectContaining({
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ name: "Recipient A" }),
      }),
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "http://localhost:5080/api/v1/household",
      expect.objectContaining({ method: "PUT", body: JSON.stringify({ name: "Home" }) }),
    );
  });

  it("sends no body and no content type when there is nothing to send", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    const fetchMock = stubFetch(Response.json({}));

    await apiPost("/recipients/1/archive");

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(init.body).toBeUndefined();
    expect(init.headers).toEqual({ Accept: "application/json" });
  });

  it("treats 204 No Content as success with no body", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    const fetchMock = stubFetch(new Response(null, { status: 204 }));

    await expect(apiDelete("/milk-rates/1")).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5080/api/v1/milk-rates/1",
      expect.objectContaining({ method: "DELETE" }),
    );
  });

  it("does not call the API when its address is not configured", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "");
    const fetchMock = stubFetch(Response.json({}));

    await expect(apiPost("/recipients", {})).rejects.toMatchObject({ kind: "not-configured" });
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

describe("Problem Details", () => {
  it("keeps the problem body, with its code, on the error", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(
      problemResponse(409, {
        status: 409,
        title: "Conflict",
        detail: "Another recipient who is not archived is already called 'Recipient A'.",
        code: "recipient-name-taken",
      }),
    );

    const error = await apiPost("/recipients", {}).catch((caught: unknown) => caught);

    expect(error).toMatchObject({ kind: "http", status: 409 });
    expect((error as ApiError).problem?.detail).toContain("already called");
    expect(hasProblemCode(error, "recipient-name-taken")).toBe(true);
    expect(hasProblemCode(error, "rate-date-taken")).toBe(false);
  });

  it("reads the first message for each invalid field", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(
      problemResponse(400, {
        status: 400,
        title: "One or more validation errors occurred.",
        code: "validation-failed",
        errors: {
          name: ["The recipient's name is required.", "Another message."],
          expectedMl: ["Too small."],
        },
      }),
    );

    const error = await apiPost("/recipients", {}).catch((caught: unknown) => caught);

    expect(fieldErrors(error)).toEqual({
      name: "The recipient's name is required.",
      expectedMl: "Too small.",
    });
  });

  it("copes with an error body that is not JSON", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(
      new Response("<html>Bad gateway</html>", {
        status: 502,
        headers: { "Content-Type": "text/html" },
      }),
    );

    const error = await apiGet("/status").catch((caught: unknown) => caught);

    expect(error).toMatchObject({ kind: "http", status: 502 });
    expect(fieldErrors(error)).toEqual({});
    expect(hasProblemCode(error, "anything")).toBe(false);
  });

  it("copes with JSON that does not parse", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(
      new Response("{ nope", {
        status: 500,
        headers: { "Content-Type": "application/problem+json" },
      }),
    );

    const error = await apiGet("/status").catch((caught: unknown) => caught);

    expect(error).toMatchObject({ kind: "http", status: 500 });
    expect((error as ApiError).problem).toBeUndefined();
  });

  it("returns no field errors for errors that are not API errors", () => {
    expect(fieldErrors(new Error("boom"))).toEqual({});
    expect(fieldErrors(undefined)).toEqual({});
  });
});
