import { describe, expect, it } from "vitest";
import { MAX_PRICE_PAISE, formatPaise, formatPricePerLitre, parseRupees } from "@/lib/money";

describe("formatPaise", () => {
  it("shows rupees and paise", () => {
    expect(formatPaise(6250)).toBe("₹62.50");
    expect(formatPaise(6000)).toBe("₹60.00");
    expect(formatPaise(1)).toBe("₹0.01");
  });

  it("groups large amounts the Indian way", () => {
    expect(formatPaise(1_000_000)).toBe("₹10,000.00");
    expect(formatPaise(12_345_600)).toBe("₹1,23,456.00");
  });
});

describe("formatPricePerLitre", () => {
  it("adds the unit", () => {
    expect(formatPricePerLitre(6250)).toBe("₹62.50 a litre");
  });
});

describe("parseRupees", () => {
  it.each([
    ["62.50", 6250],
    ["62.5", 6250],
    ["62", 6200],
    ["0.01", 1],
    ["0.1", 10],
    ["10000", 1_000_000],
    ["₹62.50", 6250],
    ["  ₹ 62  ", 6200],
  ])("reads %j as %i paise", (text, paise) => {
    expect(parseRupees(text)).toEqual({ ok: true, paise });
  });

  it("converts without floating-point error", () => {
    // 0.07 * 100 is 7.000000000000001 in floating point.
    expect(parseRupees("0.07")).toEqual({ ok: true, paise: 7 });
    expect(parseRupees("19.99")).toEqual({ ok: true, paise: 1999 });
  });

  it("asks for a price when the text is empty", () => {
    expect(parseRupees("   ")).toMatchObject({
      ok: false,
      message: expect.stringContaining("Enter"),
    });
  });

  it.each(["abc", "62,50", "62.505", "-5", "1e3", "6 2", ".5", "62.", "₹₹62"])(
    "rejects %j as not a price",
    (text) => {
      expect(parseRupees(text)).toMatchObject({
        ok: false,
        message: expect.stringContaining("numbers only"),
      });
    },
  );

  it("rejects zero and anything above the largest allowed price", () => {
    for (const text of ["0", "0.00", "10000.01", "99999"]) {
      expect(parseRupees(text)).toMatchObject({
        ok: false,
        message: expect.stringContaining("between"),
      });
    }
    expect(MAX_PRICE_PAISE).toBe(1_000_000);
  });
});
