// @vitest-environment node
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiGet, getApiBaseUrl } from "@/services/api-client";
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
      service: "milk-manager-api",
      version: "0.1.0",
      serverTime: "2026-01-15T06:30:00Z",
    };
    const fetchMock = stubFetch(Response.json(body));

    await expect(getApiStatus()).resolves.toEqual(body);
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5080/api/v1/status",
      expect.objectContaining({ headers: { Accept: "application/json" } }),
    );
  });

  it("reports HTTP errors with their status", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(new Response(null, { status: 503 }));

    const error = await apiGet("/status").catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ kind: "http", status: 503 });
  });

  it("reports network failures as unreachable", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    stubFetch(new TypeError("fetch failed"));

    await expect(apiGet("/status")).rejects.toMatchObject({ kind: "unreachable" });
  });
});
