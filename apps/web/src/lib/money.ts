/**
 * Display and input helpers for prices. Money is whole paise everywhere, matching PureFreshMilk.Domain's
 * Money and PricePerLitre (ADR 0004). These helpers format and parse text; they never do billing
 * arithmetic, which the server owns.
 */

export const MIN_PRICE_PAISE = 1;
export const MAX_PRICE_PAISE = 1_000_000;

const rupees = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR" });

/** 6250 → "₹62.50". */
export function formatPaise(paise: number): string {
  return rupees.format(paise / 100);
}

/** 6250 → "₹62.50 a litre". */
export function formatPricePerLitre(paisePerLitre: number): string {
  return `${formatPaise(paisePerLitre)} a litre`;
}

export type ParsedPrice = { ok: true; paise: number } | { ok: false; message: string };

const PRICE_PATTERN = /^(\d{1,5})(?:\.(\d{1,2}))?$/;

/**
 * Reads what a person types for the price of a litre in rupees, for example "62.50", "62.5", "₹62" or
 * "62". Only digits and up to two decimals are accepted, and the text is turned into paise without
 * floating-point arithmetic.
 */
export function parseRupees(input: string): ParsedPrice {
  const text = input.replace(/^\s*₹/, "").trim();

  if (text === "") {
    return { ok: false, message: "Enter the price of one litre, for example 62.50." };
  }

  const match = PRICE_PATTERN.exec(text);
  if (!match) {
    return { ok: false, message: "Use numbers only, with up to two decimals, for example 62.50." };
  }

  const paise = Number(match[1]) * 100 + Number((match[2] ?? "").padEnd(2, "0"));
  if (paise < MIN_PRICE_PAISE || paise > MAX_PRICE_PAISE) {
    return {
      ok: false,
      message: `Enter a price between ${formatPaise(MIN_PRICE_PAISE)} and ${formatPaise(MAX_PRICE_PAISE)} a litre.`,
    };
  }

  return { ok: true, paise };
}
