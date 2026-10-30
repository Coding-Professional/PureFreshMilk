import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RecipientsScreen } from "@/features/recipients/recipients-screen";
import { formatIsoDate } from "@/lib/dates";
import { ApiError } from "@/services/api-client";
import { getHousehold, setUpHousehold } from "@/services/household-service";
import {
  addRecipient,
  archiveRecipient,
  listRecipients,
  unarchiveRecipient,
  updateRecipient,
} from "@/services/recipient-service";
import type { ApiHousehold, ApiProblem, ApiRecipient } from "@/types/api";

vi.mock("@/services/household-service", () => ({ getHousehold: vi.fn(), setUpHousehold: vi.fn() }));
vi.mock("@/services/recipient-service", () => ({
  listRecipients: vi.fn(),
  addRecipient: vi.fn(),
  updateRecipient: vi.fn(),
  archiveRecipient: vi.fn(),
  unarchiveRecipient: vi.fn(),
}));

const household: ApiHousehold = {
  id: "h1",
  name: "Example household",
  timeZoneId: "Asia/Kolkata",
  today: "2026-10-30",
};

const recipientA: ApiRecipient = {
  id: "r-a",
  name: "Recipient A",
  expectedMl: 1000,
  startsOn: "2026-10-01",
  archivedFrom: null,
};

const recipientB: ApiRecipient = {
  id: "r-b",
  name: "Recipient B",
  expectedMl: 500,
  startsOn: "2026-10-30",
  archivedFrom: null,
};

function httpError(status: number, problem?: ApiProblem) {
  return new ApiError(`The API responded with HTTP ${status}.`, "http", status, { problem });
}

const notSetUp = () => httpError(404, { status: 404, code: "household-not-set-up" });

beforeEach(() => {
  vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
  vi.mocked(getHousehold).mockResolvedValue(household);
  vi.mocked(listRecipients).mockResolvedValue([recipientA]);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetAllMocks();
});

describe("RecipientsScreen without an API address", () => {
  it("shows the sample household, labelled as demo data, and never calls the API", async () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "");

    render(<RecipientsScreen />);

    expect(screen.getByRole("heading", { level: 1, name: "Recipients" })).toBeInTheDocument();
    expect(screen.getByText("Demo data")).toBeInTheDocument();
    expect(screen.getByText("Recipient A")).toBeInTheDocument();
    expect(screen.getByText("Recipient C")).toBeInTheDocument();
    expect(screen.getByText(/not connected to an API/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Add recipient" })).not.toBeInTheDocument();
    expect(getHousehold).not.toHaveBeenCalled();
    expect(listRecipients).not.toHaveBeenCalled();
  });
});

describe("RecipientsScreen with an API", () => {
  it("says it is loading until the recipients arrive", async () => {
    vi.mocked(getHousehold).mockReturnValue(new Promise(() => {}));
    vi.mocked(listRecipients).mockReturnValue(new Promise(() => {}));

    render(<RecipientsScreen />);

    expect(screen.getByRole("status")).toHaveTextContent("Loading recipients");
    expect(screen.queryByText("Demo data")).not.toBeInTheDocument();
  });

  it("lists the household's recipients with their usual quantity, and no demo label", async () => {
    vi.mocked(listRecipients).mockResolvedValue([recipientA, recipientB]);

    render(<RecipientsScreen />);

    expect(await screen.findByText("Recipient A")).toBeInTheDocument();
    expect(screen.getByText("Recipient B")).toBeInTheDocument();
    expect(screen.getByText("1 L a day")).toBeInTheDocument();
    expect(screen.getByText(/500 ml a day/)).toBeInTheDocument();
    expect(screen.queryByText("Demo data")).not.toBeInTheDocument();
    expect(listRecipients).toHaveBeenCalledWith(
      expect.objectContaining({ includeArchived: false }),
    );
  });

  it("says when a recipient has not started yet", async () => {
    vi.mocked(listRecipients).mockResolvedValue([
      recipientB,
      { ...recipientA, startsOn: "2026-11-05" },
    ]);

    render(<RecipientsScreen />);

    expect(
      await screen.findByText(new RegExp(`starts ${formatIsoDate("2026-11-05")}`)),
    ).toBeInTheDocument();
    expect(
      screen.queryByText(new RegExp(`starts ${formatIsoDate("2026-10-30")}`)),
    ).not.toBeInTheDocument();
  });

  it("invites you to add the first recipient when there are none", async () => {
    vi.mocked(listRecipients).mockResolvedValue([]);

    render(<RecipientsScreen />);

    expect(await screen.findByRole("heading", { name: "No recipients yet" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add the first recipient" })).toBeInTheDocument();
  });

  it("asks you to set up the household first, then shows the recipients", async () => {
    vi.mocked(getHousehold).mockRejectedValueOnce(notSetUp()).mockResolvedValue(household);
    vi.mocked(listRecipients).mockRejectedValueOnce(notSetUp()).mockResolvedValue([]);
    vi.mocked(setUpHousehold).mockResolvedValue(household);
    const user = userEvent.setup();

    render(<RecipientsScreen />);

    expect(
      await screen.findByRole("heading", { name: "Set up your household first" }),
    ).toBeInTheDocument();
    await user.type(screen.getByLabelText("Household name"), "Example household");
    await user.click(screen.getByRole("button", { name: "Create household" }));

    expect(setUpHousehold).toHaveBeenCalledWith({
      name: "Example household",
      timeZoneId: "Asia/Kolkata",
    });
    expect(await screen.findByRole("heading", { name: "No recipients yet" })).toBeInTheDocument();
  });

  it("shows the API's message under the household name field", async () => {
    vi.mocked(getHousehold).mockRejectedValue(notSetUp());
    vi.mocked(listRecipients).mockRejectedValue(notSetUp());
    vi.mocked(setUpHousehold).mockRejectedValue(
      httpError(400, {
        status: 400,
        code: "validation-failed",
        errors: { name: ["The household name is required."] },
      }),
    );
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    await user.click(await screen.findByRole("button", { name: "Create household" }));

    expect(await screen.findByText("The household name is required.")).toBeInTheDocument();
    expect(screen.getByLabelText("Household name")).toBeInvalid();
  });

  it("explains an unreachable API and loads again when you try again", async () => {
    vi.mocked(getHousehold)
      .mockRejectedValueOnce(new ApiError("down", "unreachable"))
      .mockResolvedValue(household);
    vi.mocked(listRecipients).mockRejectedValueOnce(new ApiError("down", "unreachable"));
    const user = userEvent.setup();

    render(<RecipientsScreen />);

    expect(
      await screen.findByText(/Can't reach the API at http:\/\/localhost:5080/),
    ).toBeInTheDocument();
    expect(screen.queryByText("Recipient A")).not.toBeInTheDocument();
    expect(screen.queryByText("Demo data")).not.toBeInTheDocument();

    vi.mocked(listRecipients).mockResolvedValue([recipientA]);
    await user.click(screen.getByRole("button", { name: "Try again" }));

    expect(await screen.findByText("Recipient A")).toBeInTheDocument();
  });

  it("shows the server's explanation for other failures", async () => {
    vi.mocked(getHousehold).mockRejectedValue(httpError(500));

    render(<RecipientsScreen />);

    expect(await screen.findByText(/The API answered with HTTP 500/)).toBeInTheDocument();
  });
});

describe("adding a recipient", () => {
  async function openAddForm(user: ReturnType<typeof userEvent.setup>) {
    await user.click(await screen.findByRole("button", { name: "Add recipient" }));
    return screen.getByRole("form", { name: "Add a recipient" });
  }

  it("saves the recipient, closes the form and shows the new list", async () => {
    vi.mocked(listRecipients)
      .mockResolvedValueOnce([recipientA])
      .mockResolvedValue([recipientA, recipientB]);
    vi.mocked(addRecipient).mockResolvedValue(recipientB);
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);

    expect(within(form).getByLabelText("First delivery on")).toHaveValue("2026-10-30"); // today, from the server
    expect(within(form).getByLabelText("Name")).toHaveFocus();

    await user.type(within(form).getByLabelText("Name"), "Recipient B");
    await user.type(within(form).getByLabelText("Usual daily quantity (ml)"), "500");
    expect(within(form).getByText("That is 500 ml a day.")).toBeInTheDocument();
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(addRecipient).toHaveBeenCalledWith({
      name: "Recipient B",
      expectedMl: 500,
      startsOn: "2026-10-30",
    });
    expect(await screen.findByText("Recipient B")).toBeInTheDocument();
    expect(screen.queryByRole("form", { name: "Add a recipient" })).not.toBeInTheDocument();
  });

  it("shows the API's messages under the fields they are about and keeps what was typed", async () => {
    vi.mocked(addRecipient).mockRejectedValue(
      httpError(400, {
        status: 400,
        code: "validation-failed",
        errors: {
          name: ["The recipient's name is required."],
          expectedMl: ["The usual quantity must be between 1 ml and 20000 ml (got 30000 ml)."],
        },
      }),
    );
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);
    await user.type(within(form).getByLabelText("Usual daily quantity (ml)"), "30000");
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(await screen.findByText("The recipient's name is required.")).toBeInTheDocument();
    expect(screen.getByText(/must be between 1 ml and 20000 ml/)).toBeInTheDocument();
    expect(within(form).getByLabelText("Name")).toBeInvalid();
    expect(within(form).getByLabelText("Usual daily quantity (ml)")).toHaveValue("30000");
  });

  it("explains a name that is already taken", async () => {
    vi.mocked(addRecipient).mockRejectedValue(
      httpError(409, {
        status: 409,
        code: "recipient-name-taken",
        detail: "Another recipient ...",
      }),
    );
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);
    await user.type(within(form).getByLabelText("Name"), "Recipient A");
    await user.type(within(form).getByLabelText("Usual daily quantity (ml)"), "500");
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(await screen.findByText(/already called that/)).toBeInTheDocument();
  });

  it("checks the quantity is a whole number before calling the API", async () => {
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);
    await user.type(within(form).getByLabelText("Name"), "Recipient B");
    await user.type(within(form).getByLabelText("Usual daily quantity (ml)"), "1.5 litres");
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(
      await screen.findByText("Enter a whole number of millilitres, for example 500."),
    ).toBeInTheDocument();
    expect(addRecipient).not.toHaveBeenCalled();
  });

  it("shows a message in the form when the API cannot be reached", async () => {
    vi.mocked(addRecipient).mockRejectedValue(new ApiError("down", "unreachable"));
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);
    await user.type(within(form).getByLabelText("Name"), "Recipient B");
    await user.type(within(form).getByLabelText("Usual daily quantity (ml)"), "500");
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(await within(form).findByRole("alert")).toHaveTextContent("Can't reach the API");
    expect(within(form).getByRole("button", { name: "Save recipient" })).toBeEnabled();
  });

  it("closes the form without saving when you cancel", async () => {
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    const form = await openAddForm(user);
    await user.click(within(form).getByRole("button", { name: "Cancel" }));

    expect(screen.queryByRole("form", { name: "Add a recipient" })).not.toBeInTheDocument();
    expect(addRecipient).not.toHaveBeenCalled();
  });
});

describe("changing a recipient", () => {
  it("edits the details in place", async () => {
    vi.mocked(updateRecipient).mockResolvedValue({ ...recipientA, name: "Recipient Z" });
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    await user.click(await screen.findByRole("button", { name: "Edit Recipient A" }));
    const form = screen.getByRole("form", { name: "Edit Recipient A" });

    expect(within(form).getByLabelText("Name")).toHaveValue("Recipient A");
    expect(within(form).getByLabelText("Usual daily quantity (ml)")).toHaveValue("1000");
    expect(within(form).getByLabelText("First delivery on")).toHaveValue("2026-10-01");

    await user.clear(within(form).getByLabelText("Name"));
    await user.type(within(form).getByLabelText("Name"), "Recipient Z");
    await user.click(within(form).getByRole("button", { name: "Save recipient" }));

    expect(updateRecipient).toHaveBeenCalledWith("r-a", {
      name: "Recipient Z",
      expectedMl: 1000,
      startsOn: "2026-10-01",
    });
    expect(screen.queryByRole("form", { name: "Edit Recipient A" })).not.toBeInTheDocument();
  });

  it("archives only after you confirm, and keeps the recipient if you change your mind", async () => {
    vi.mocked(archiveRecipient).mockResolvedValue({ ...recipientA, archivedFrom: "2026-10-30" });
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    await user.click(await screen.findByRole("button", { name: "Archive Recipient A" }));
    const question = screen.getByRole("group", { name: "Archive Recipient A?" });

    expect(archiveRecipient).not.toHaveBeenCalled();
    expect(within(question).getByText(/Their history is kept/)).toBeInTheDocument();

    await user.click(within(question).getByRole("button", { name: "Keep" }));
    expect(screen.queryByRole("group", { name: "Archive Recipient A?" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Archive Recipient A" }));
    await user.click(
      within(screen.getByRole("group", { name: "Archive Recipient A?" })).getByRole("button", {
        name: "Archive Recipient A",
      }),
    );

    expect(archiveRecipient).toHaveBeenCalledTimes(1);
    expect(archiveRecipient).toHaveBeenCalledWith("r-a");
    expect(listRecipients).toHaveBeenCalledTimes(2); // refreshed after saving
  });

  it("shows what went wrong when archiving fails", async () => {
    vi.mocked(archiveRecipient).mockRejectedValue(
      httpError(409, {
        status: 409,
        code: "recipient-already-archived",
        detail: "The recipient is already archived.",
      }),
    );
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    await user.click(await screen.findByRole("button", { name: "Archive Recipient A" }));
    await user.click(
      within(screen.getByRole("group", { name: "Archive Recipient A?" })).getByRole("button", {
        name: "Archive Recipient A",
      }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The recipient is already archived.",
    );
  });

  it("says why archiving was refused when the API names a field but gives no explanation", async () => {
    vi.mocked(archiveRecipient).mockRejectedValue(
      httpError(400, {
        status: 400,
        code: "validation-failed",
        errors: { archivedFrom: ["A recipient must be active for at least one day."] },
      }),
    );
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    await user.click(await screen.findByRole("button", { name: "Archive Recipient A" }));
    await user.click(
      within(screen.getByRole("group", { name: "Archive Recipient A?" })).getByRole("button", {
        name: "Archive Recipient A",
      }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "A recipient must be active for at least one day.",
    );
  });

  it("shows archived recipients on request and restores one", async () => {
    const archived: ApiRecipient = { ...recipientA, archivedFrom: "2026-10-15" };
    vi.mocked(listRecipients).mockImplementation(async ({ includeArchived } = {}) =>
      includeArchived ? [archived, recipientB] : [recipientB],
    );
    vi.mocked(unarchiveRecipient).mockResolvedValue(recipientA);
    const user = userEvent.setup();

    render(<RecipientsScreen />);
    expect(await screen.findByText("Recipient B")).toBeInTheDocument();
    expect(screen.queryByText("Recipient A")).not.toBeInTheDocument();

    await user.click(screen.getByRole("checkbox", { name: "Show archived recipients" }));

    expect(await screen.findByText("Recipient A")).toBeInTheDocument();
    expect(screen.getByText(`Archived from ${formatIsoDate("2026-10-15")}`)).toBeInTheDocument();
    expect(listRecipients).toHaveBeenLastCalledWith(
      expect.objectContaining({ includeArchived: true }),
    );

    await user.click(screen.getByRole("button", { name: "Restore Recipient A" }));

    expect(unarchiveRecipient).toHaveBeenCalledWith("r-a");
  });
});
