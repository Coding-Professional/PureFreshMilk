import { describe, expect, it } from "vitest";
import { recipientMarker } from "@/lib/recipient-marker";

describe("recipientMarker", () => {
  it.each([
    ["Recipient A", "A"],
    ["Asha", "A"],
    ["asha rao", "AR"],
    ["  Ground floor  flat ", "GF"],
    ["   ", "?"],
  ])("%j → %s", (name, expected) => {
    expect(recipientMarker(name)).toBe(expected);
  });
});
