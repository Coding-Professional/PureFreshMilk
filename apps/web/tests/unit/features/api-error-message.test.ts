import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { describeApiError } from "@/features/shared/api-error-message";
import { ApiError } from "@/services/api-client";

beforeEach(() => vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080"));
afterEach(() => vi.unstubAllEnvs());

describe("describeApiError", () => {
  it("tells you how to set the API address when there is none", () => {
    expect(describeApiError(new ApiError("x", "not-configured"))).toMatch(
      /NEXT_PUBLIC_API_BASE_URL/,
    );
  });

  it("names the address it could not reach", () => {
    expect(describeApiError(new ApiError("x", "unreachable"))).toBe(
      "Can't reach the API at http://localhost:5080. Start it (see docs/development.md), then try again.",
    );
  });

  it("uses the API's own explanation when it gave one", () => {
    const error = new ApiError("x", "http", 409, {
      problem: { status: 409, detail: "The recipient is already archived." },
    });

    expect(describeApiError(error)).toBe("The recipient is already archived.");
  });

  it("falls back to the first validation message when there is no explanation", () => {
    const error = new ApiError("x", "http", 400, {
      problem: {
        status: 400,
        errors: { archivedFrom: ["A recipient must be active for at least one day."] },
      },
    });

    expect(describeApiError(error)).toBe("A recipient must be active for at least one day.");
  });

  it("falls back to the status when the API said nothing useful", () => {
    expect(describeApiError(new ApiError("x", "http", 500))).toBe(
      "The API answered with HTTP 500. Check the API logs.",
    );
  });

  it("copes with errors that are not API errors", () => {
    expect(describeApiError(new Error("boom"))).toMatch(/Something went wrong/);
  });
});
