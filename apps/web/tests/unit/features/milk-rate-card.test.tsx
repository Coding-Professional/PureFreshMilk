import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MilkRateCard } from "@/features/settings/milk-rate-card";
import { formatIsoDate } from "@/lib/dates";
import { ApiError } from "@/services/api-client";
import { getHousehold } from "@/services/household-service";
import {
  addMilkRate,
  deleteMilkRate,
  getMilkRateInEffect,
  listMilkRates,
} from "@/services/milk-rate-service";
import type { ApiHousehold, ApiMilkRate, ApiProblem } from "@/types/api";

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

const october: ApiMilkRate = { id: "m1", effectiveFrom: "2026-10-01", pricePerLitrePaise: 6000 };
const midOctober: ApiMilkRate = { id: "m2", effectiveFrom: "2026-10-15", pricePerLitrePaise: 6400 };

function httpError(status: number, problem?: ApiProblem) {
  return new ApiError(`The API responded with HTTP ${status}.`, "http", status, { problem });
}

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
  vi.mocked(getHousehold).mockResolvedValue(household);
  vi.mocked(listMilkRates).mockResolvedValue([midOctober, october]);
  vi.mocked(getMilkRateInEffect).mockResolvedValue(midOctober);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetAllMocks();
});

describe("MilkRateCard", () => {
  it("says it is loading until the prices arrive", () => {
    vi.mocked(getHousehold).mockReturnValue(new Promise(() => {}));

    render(<MilkRateCard />);

    expect(screen.getByRole("status")).toHaveTextContent("Loading prices");
  });

  it("shows the price in force, as the server decides it, and the history", async () => {
    render(<MilkRateCard />);

    expect(await screen.findByText("₹64.00 a litre", { selector: "span" })).toBeInTheDocument();
    expect(screen.getByText(`since ${formatIsoDate("2026-10-15")}`)).toBeInTheDocument();

    const history = screen.getByRole("list");
    const items = within(history).getAllByRole("listitem");
    expect(items).toHaveLength(2);
    expect(within(items[0]!).getByText("In effect")).toBeInTheDocument(); // the 15 October price
    expect(within(items[1]!).queryByText("In effect")).not.toBeInTheDocument();
    expect(getMilkRateInEffect).toHaveBeenCalledWith(undefined, expect.anything());
  });

  it("says plainly that days with no price are not free", async () => {
    vi.mocked(listMilkRates).mockResolvedValue([]);
    vi.mocked(getMilkRateInEffect).mockResolvedValue(null);

    render(<MilkRateCard />);

    expect(await screen.findByText(/No price is in force yet/)).toBeInTheDocument();
    expect(screen.getByText(/never counted as free/)).toBeInTheDocument();
    expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });

  it("points to the household section when there is no household yet", async () => {
    vi.mocked(getHousehold).mockRejectedValue(
      httpError(404, { status: 404, code: "household-not-set-up" }),
    );
    vi.mocked(listMilkRates).mockRejectedValue(
      httpError(404, { status: 404, code: "household-not-set-up" }),
    );

    render(<MilkRateCard />);

    expect(await screen.findByText("Set up your household first")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Add price" })).not.toBeInTheDocument();
  });

  it("explains an unreachable API and loads again when you try again", async () => {
    vi.mocked(getHousehold).mockRejectedValueOnce(new ApiError("down", "unreachable"));
    const user = userEvent.setup();

    render(<MilkRateCard />);
    expect(await screen.findByText(/Can't reach the API/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByText("₹64.00 a litre", { selector: "span" })).toBeInTheDocument();
  });

  it("adds a price in paise, starting today unless told otherwise", async () => {
    vi.mocked(addMilkRate).mockResolvedValue({
      id: "m3",
      effectiveFrom: "2026-10-30",
      pricePerLitrePaise: 6250,
    });
    const user = userEvent.setup();

    render(<MilkRateCard />);
    const form = await screen.findByRole("form", { name: "Add a price" });

    expect(within(form).getByLabelText("Applies from")).toHaveValue("2026-10-30");
    await user.type(within(form).getByLabelText("Price of one litre (₹)"), "62.50");
    await user.click(within(form).getByRole("button", { name: "Add price" }));

    expect(addMilkRate).toHaveBeenCalledWith({
      effectiveFrom: "2026-10-30",
      pricePerLitrePaise: 6250,
    });
    expect(listMilkRates).toHaveBeenCalledTimes(2); // refreshed after saving
  });

  it("can back-date a price", async () => {
    vi.mocked(addMilkRate).mockResolvedValue(october);
    const user = userEvent.setup();

    render(<MilkRateCard />);
    const form = await screen.findByRole("form", { name: "Add a price" });
    await user.type(within(form).getByLabelText("Price of one litre (₹)"), "60");
    await user.clear(within(form).getByLabelText("Applies from"));
    await user.type(within(form).getByLabelText("Applies from"), "2026-09-01");
    await user.click(within(form).getByRole("button", { name: "Add price" }));

    expect(addMilkRate).toHaveBeenCalledWith({
      effectiveFrom: "2026-09-01",
      pricePerLitrePaise: 6000,
    });
  });

  it("checks the price before calling the API", async () => {
    const user = userEvent.setup();

    render(<MilkRateCard />);
    const form = await screen.findByRole("form", { name: "Add a price" });
    await user.type(within(form).getByLabelText("Price of one litre (₹)"), "sixty");
    await user.click(within(form).getByRole("button", { name: "Add price" }));

    expect(await screen.findByText(/Use numbers only/)).toBeInTheDocument();
    expect(within(form).getByLabelText("Price of one litre (₹)")).toBeInvalid();
    expect(addMilkRate).not.toHaveBeenCalled();
  });

  it("explains a day that already has a price", async () => {
    vi.mocked(addMilkRate).mockRejectedValue(
      httpError(409, {
        status: 409,
        code: "rate-date-taken",
        detail: "A price already starts on 2026-10-01.",
      }),
    );
    const user = userEvent.setup();

    render(<MilkRateCard />);
    const form = await screen.findByRole("form", { name: "Add a price" });
    await user.type(within(form).getByLabelText("Price of one litre (₹)"), "62.50");
    await user.click(within(form).getByRole("button", { name: "Add price" }));

    expect(await screen.findByText(/A price already starts on that day/)).toBeInTheDocument();
  });

  it("shows the API's message for an invalid price", async () => {
    vi.mocked(addMilkRate).mockRejectedValue(
      httpError(400, {
        status: 400,
        code: "validation-failed",
        errors: {
          pricePerLitrePaise: ["A price must be between 1 and 1000000 paise a litre (got 0)."],
        },
      }),
    );
    const user = userEvent.setup();

    render(<MilkRateCard />);
    const form = await screen.findByRole("form", { name: "Add a price" });
    await user.type(within(form).getByLabelText("Price of one litre (₹)"), "62.50");
    await user.click(within(form).getByRole("button", { name: "Add price" }));

    expect(
      await screen.findByText(/A price must be between 1 and 1000000 paise/),
    ).toBeInTheDocument();
  });

  it("removes a price only after you confirm", async () => {
    vi.mocked(deleteMilkRate).mockResolvedValue(undefined);
    const user = userEvent.setup();

    render(<MilkRateCard />);
    await user.click(
      await screen.findByRole("button", {
        name: `Remove the price from ${formatIsoDate("2026-10-01")}`,
      }),
    );
    const question = screen.getByRole("group", { name: "Confirm removing the price" });

    expect(deleteMilkRate).not.toHaveBeenCalled();
    await user.click(within(question).getByRole("button", { name: "Keep" }));
    expect(
      screen.queryByRole("group", { name: "Confirm removing the price" }),
    ).not.toBeInTheDocument();

    await user.click(
      screen.getByRole("button", { name: `Remove the price from ${formatIsoDate("2026-10-01")}` }),
    );
    await user.click(
      within(screen.getByRole("group", { name: "Confirm removing the price" })).getByRole(
        "button",
        {
          name: "Remove price",
        },
      ),
    );

    expect(deleteMilkRate).toHaveBeenCalledWith("m1");
    expect(listMilkRates).toHaveBeenCalledTimes(2);
  });

  it("shows what went wrong when removing fails", async () => {
    vi.mocked(deleteMilkRate).mockRejectedValue(
      httpError(404, { status: 404, detail: "There is no such price." }),
    );
    const user = userEvent.setup();

    render(<MilkRateCard />);
    await user.click(
      await screen.findByRole("button", {
        name: `Remove the price from ${formatIsoDate("2026-10-01")}`,
      }),
    );
    await user.click(screen.getByRole("button", { name: "Remove price" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("There is no such price.");
  });
});
