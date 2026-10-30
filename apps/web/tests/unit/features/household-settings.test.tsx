import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HouseholdSettings } from "@/features/settings/household-settings";
import { ApiError } from "@/services/api-client";
import { getHousehold, setUpHousehold } from "@/services/household-service";
import { getMilkRateInEffect, listMilkRates } from "@/services/milk-rate-service";
import type { ApiHousehold } from "@/types/api";

vi.mock("@/services/household-service", () => ({ getHousehold: vi.fn(), setUpHousehold: vi.fn() }));
vi.mock("@/services/milk-rate-service", () => ({
  listMilkRates: vi.fn(),
  getMilkRateInEffect: vi.fn(),
  addMilkRate: vi.fn(),
  deleteMilkRate: vi.fn(),
}));

const household: ApiHousehold = {
  id: "h1",
  name: "Example household",
  timeZoneId: "Asia/Kolkata",
  today: "2026-10-30",
};

beforeEach(() => {
  vi.mocked(getHousehold).mockResolvedValue(household);
  vi.mocked(listMilkRates).mockResolvedValue([]);
  vi.mocked(getMilkRateInEffect).mockResolvedValue(null);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetAllMocks();
});

describe("HouseholdSettings without an API address", () => {
  it("says the settings need the API instead of showing forms that cannot work", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "");

    render(<HouseholdSettings />);

    expect(screen.getByText("Household and price settings need the API")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save household" })).not.toBeInTheDocument();
    expect(getHousehold).not.toHaveBeenCalled();
  });
});

describe("HouseholdSettings with an API", () => {
  beforeEach(() => vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080"));

  it("shows the household's name and time zone, ready to change", async () => {
    render(<HouseholdSettings />);

    expect(await screen.findByLabelText("Household name")).toHaveValue("Example household");
    expect(screen.getByLabelText("Time zone")).toHaveValue("Asia/Kolkata");
    expect(screen.getByRole("heading", { name: "Milk price" })).toBeInTheDocument();
  });

  it("saves a new name and time zone", async () => {
    vi.mocked(setUpHousehold).mockResolvedValue({
      ...household,
      name: "Renamed",
      timeZoneId: "Europe/London",
    });
    const user = userEvent.setup();

    render(<HouseholdSettings />);
    const name = await screen.findByLabelText("Household name");
    await user.clear(name);
    await user.type(name, "Renamed");
    await user.selectOptions(screen.getByLabelText("Time zone"), "Europe/London");
    await user.click(screen.getByRole("button", { name: "Save household" }));

    expect(setUpHousehold).toHaveBeenCalledWith({ name: "Renamed", timeZoneId: "Europe/London" });
    // The household card loaded again to show what was saved; the price card loaded once, at the start.
    expect(getHousehold).toHaveBeenCalledTimes(3);
  });

  it("keeps a time zone that is not in the short list", async () => {
    vi.mocked(getHousehold).mockResolvedValue({ ...household, timeZoneId: "Africa/Nairobi" });

    render(<HouseholdSettings />);

    expect(await screen.findByLabelText("Time zone")).toHaveValue("Africa/Nairobi");
  });

  it("offers to create the household when there is none", async () => {
    vi.mocked(getHousehold).mockRejectedValue(
      new ApiError("missing", "http", 404, {
        problem: { status: 404, code: "household-not-set-up" },
      }),
    );
    vi.mocked(listMilkRates).mockRejectedValue(
      new ApiError("missing", "http", 404, {
        problem: { status: 404, code: "household-not-set-up" },
      }),
    );

    render(<HouseholdSettings />);

    expect(await screen.findByText(/Not set up yet/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create household" })).toBeInTheDocument();
    expect(screen.getByText("Set up your household first")).toBeInTheDocument();
  });

  it("shows the API's message under the field it is about", async () => {
    vi.mocked(setUpHousehold).mockRejectedValue(
      new ApiError("bad", "http", 400, {
        problem: {
          status: 400,
          code: "validation-failed",
          errors: { name: ["The household name is required."] },
        },
      }),
    );
    const user = userEvent.setup();

    render(<HouseholdSettings />);
    const name = await screen.findByLabelText("Household name");
    await user.clear(name);
    await user.click(screen.getByRole("button", { name: "Save household" }));

    expect(await screen.findByText("The household name is required.")).toBeInTheDocument();
  });
});
