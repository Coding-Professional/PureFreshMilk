/**
 * Display helpers for milk quantities. Quantities are always whole millilitres,
 * matching MilkManager.Domain's Quantity value object.
 */

export const MILLILITRES_PER_LITRE = 1000;

const litreFormat = new Intl.NumberFormat("en", { maximumFractionDigits: 3 });

/** 0 → "0 L", 500 → "500 ml", 1000 → "1 L", 1250 → "1.25 L". */
export function formatQuantity(millilitres: number): string {
  if (millilitres === 0 || millilitres >= MILLILITRES_PER_LITRE) {
    return `${litreFormat.format(millilitres / MILLILITRES_PER_LITRE)} L`;
  }
  return `${millilitres} ml`;
}

/** Spelled-out form for screen readers: 0 → "0 litres", 500 → "500 millilitres", 1000 → "1 litre". */
export function formatQuantitySpoken(millilitres: number): string {
  if (millilitres === 0 || millilitres >= MILLILITRES_PER_LITRE) {
    const litres = millilitres / MILLILITRES_PER_LITRE;
    return `${litreFormat.format(litres)} ${litres === 1 ? "litre" : "litres"}`;
  }
  return `${millilitres} ${millilitres === 1 ? "millilitre" : "millilitres"}`;
}
