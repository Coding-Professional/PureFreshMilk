import { describe, expect, it } from "vitest";
import { formatQuantity, formatQuantitySpoken } from "@/lib/quantity";

describe("formatQuantity", () => {
  it.each([
    [0, "0 L"],
    [250, "250 ml"],
    [500, "500 ml"],
    [1000, "1 L"],
    [1250, "1.25 L"],
    [1500, "1.5 L"],
    [2125, "2.125 L"],
  ])("formats %i ml as %s", (millilitres, expected) => {
    expect(formatQuantity(millilitres)).toBe(expected);
  });
});

describe("formatQuantitySpoken", () => {
  it.each([
    [0, "0 litres"],
    [1, "1 millilitre"],
    [500, "500 millilitres"],
    [1000, "1 litre"],
    [1500, "1.5 litres"],
  ])("spells out %i ml as %s", (millilitres, expected) => {
    expect(formatQuantitySpoken(millilitres)).toBe(expected);
  });
});
