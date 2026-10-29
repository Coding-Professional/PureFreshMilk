import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiStatusCard } from "@/features/settings/api-status-card";
import { ApiError } from "@/services/api-client";
import { getApiStatus } from "@/services/status-service";

vi.mock("@/services/status-service", () => ({ getApiStatus: vi.fn() }));

afterEach(() => vi.mocked(getApiStatus).mockReset());

describe("ApiStatusCard", () => {
  it("shows the API version when connected", async () => {
    vi.mocked(getApiStatus).mockResolvedValue({
      service: "purefreshmilk-api",
      version: "0.1.0",
      serverTime: "2026-01-15T06:30:00Z",
    });

    render(<ApiStatusCard />);

    expect(await screen.findByText("Connected")).toBeInTheDocument();
    expect(screen.getByText(/purefreshmilk-api 0\.1\.0/)).toBeInTheDocument();
  });

  it("explains how to fix a missing API address", async () => {
    vi.mocked(getApiStatus).mockRejectedValue(new ApiError("not set", "not-configured"));

    render(<ApiStatusCard />);

    expect(await screen.findByText(/Add NEXT_PUBLIC_API_BASE_URL/)).toBeInTheDocument();
  });
});
