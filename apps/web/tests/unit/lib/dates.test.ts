import { describe, expect, it } from "vitest";
import { formatIsoDate, isIsoDate } from "@/lib/dates";

describe("isIsoDate", () => {
  it("accepts real calendar dates", () => {
    expect(isIsoDate("2026-10-30")).toBe(true);
    expect(isIsoDate("2028-02-29")).toBe(true);
  });

  it.each([
    "2026-02-30",
    "2026-13-01",
    "2026-00-10",
    "2027-02-29",
    "30/10/2026",
    "2026-1-5",
    "",
    "today",
  ])("rejects %j", (value) => {
    expect(isIsoDate(value)).toBe(false);
  });
});

describe("formatIsoDate", () => {
  it("shows the calendar date written, whatever the machine's time zone", () => {
    expect(formatIsoDate("2026-10-30", "en-GB")).toBe("30 Oct 2026");
    expect(formatIsoDate("2026-01-01", "en-GB")).toBe("1 Jan 2026");
    expect(formatIsoDate("2026-12-31", "en-GB")).toBe("31 Dec 2026");
  });

  it("returns anything that is not a date as it is", () => {
    expect(formatIsoDate("soon", "en-GB")).toBe("soon");
    expect(formatIsoDate("2026-02-30", "en-GB")).toBe("2026-02-30");
  });
});
