import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { TodayScreen } from "@/features/today/today-screen";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";

const measure = () => screen.getByRole("img", { name: /delivered\./ });

function renderToday() {
  const user = userEvent.setup();
  render(<TodayScreen recipients={DEMO_RECIPIENTS} />);
  return user;
}

describe("TodayScreen", () => {
  it("labels the data as demo data and starts with nothing delivered", () => {
    renderToday();

    expect(screen.getByRole("heading", { level: 1, name: "Today" })).toBeInTheDocument();
    expect(screen.getByText("Demo data")).toBeInTheDocument();
    expect(measure()).toHaveAccessibleName(/^0 litres of 2 litres delivered\./);
    expect(screen.getAllByRole("article")).toHaveLength(3);
  });

  it("marks a delivery and undoes it with a second tap", async () => {
    const user = renderToday();
    const delivered = screen.getByRole("button", { name: "Delivered for Recipient A" });

    await user.click(delivered);
    expect(delivered).toHaveAttribute("aria-pressed", "true");
    expect(measure()).toHaveAccessibleName(
      /^1 litre of 2 litres delivered\. Recipient A: delivered, 1 litre\./,
    );

    await user.click(delivered);
    expect(delivered).toHaveAttribute("aria-pressed", "false");
    expect(measure()).toHaveAccessibleName(/Recipient A: not marked yet\./);
  });

  it("records a missed delivery", async () => {
    const user = renderToday();

    await user.click(screen.getByRole("button", { name: "Missed for Recipient B" }));

    expect(screen.getByRole("button", { name: "Missed for Recipient B" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(measure()).toHaveAccessibleName(/Recipient B: missed\./);
  });

  it("adjusts the quantity received", async () => {
    const user = renderToday();
    await user.click(screen.getByRole("button", { name: "Delivered for Recipient A" }));

    const card = screen.getByRole("article", { name: "Recipient A" });
    await user.click(within(card).getByRole("button", { name: "250 ml more" }));

    expect(within(card).getByRole("status")).toHaveTextContent("1.25 L");
    expect(within(card).getByText("250 ml more than usual")).toBeInTheDocument();
  });

  it("marks the rest as delivered, then starts over", async () => {
    const user = renderToday();

    await user.click(screen.getByRole("button", { name: "Mark the rest as delivered" }));
    expect(measure()).toHaveAccessibleName(/^2 litres of 2 litres delivered\./);
    expect(screen.getByRole("button", { name: "Everyone is marked" })).toBeDisabled();

    await user.click(screen.getByRole("button", { name: "Start over" }));
    expect(measure()).toHaveAccessibleName(/^0 litres of 2 litres delivered\./);
  });
});
