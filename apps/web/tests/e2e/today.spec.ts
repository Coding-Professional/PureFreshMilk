import { expect, test } from "@playwright/test";

test("records a morning and forgets it on reload", async ({ page }) => {
  await page.goto("/app/today");
  const measure = page.getByRole("img", { name: /delivered\./ });
  await expect(measure).toHaveAccessibleName(/^0 litres of 2 litres delivered\./);

  await page.getByRole("button", { name: "Missed for Recipient C" }).click();
  await page.getByRole("button", { name: "Mark the rest as delivered" }).click();

  await expect(measure).toHaveAccessibleName(
    "1.5 litres of 2 litres delivered. Recipient A: delivered, 1 litre. " +
      "Recipient B: delivered, 500 millilitres. Recipient C: missed.",
  );
  await expect(page.getByRole("button", { name: "Everyone is marked" })).toBeDisabled();

  // Demo data is never saved.
  await page.reload();
  await expect(measure).toHaveAccessibleName(/^0 litres of 2 litres delivered\./);
});
