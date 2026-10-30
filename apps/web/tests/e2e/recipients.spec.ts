import { expect, test } from "@playwright/test";

// These run against a build with no API address (see playwright.config.ts). The screens that talk to the
// API are covered by the Vitest tests with mocked service calls and by the API's own tests.

test.describe("Recipients with no API address", () => {
  test("shows sample recipients, clearly labelled as demo data", async ({ page }) => {
    await page.goto("/app/recipients");

    await expect(page.getByRole("heading", { level: 1, name: "Recipients" })).toBeVisible();
    await expect(page.getByText("Demo data", { exact: true })).toBeVisible();
    for (const name of ["Recipient A", "Recipient B", "Recipient C"]) {
      await expect(page.getByText(name, { exact: true })).toBeVisible();
    }
    await expect(page.getByText("Demo data: this app is not connected to an API")).toBeVisible();
  });

  test("offers no way to add or edit demo recipients", async ({ page }) => {
    await page.goto("/app/recipients");
    await expect(page.getByRole("heading", { level: 1, name: "Recipients" })).toBeVisible();

    await expect(page.getByRole("button", { name: /add .*recipient/i })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /^edit /i })).toHaveCount(0);
    await expect(page.getByRole("checkbox")).toHaveCount(0);
  });

  test("can be reached from the navigation", async ({ page }, testInfo) => {
    await page.goto("/app/today");
    const link =
      testInfo.project.name === "mobile-360"
        ? page
            .getByRole("navigation", { name: "Household" })
            .getByRole("link", { name: "Recipients" })
        : page.getByRole("navigation", { name: "Main" }).getByRole("link", { name: "Recipients" });

    await link.click();

    await expect(page).toHaveURL(/\/app\/recipients$/);
    await expect(page.getByRole("heading", { level: 1, name: "Recipients" })).toBeVisible();
  });
});

test.describe("Settings with no API address", () => {
  test("says the household and price settings need the API", async ({ page }) => {
    await page.goto("/app/settings");

    await expect(page.getByRole("heading", { level: 1, name: "Settings" })).toBeVisible();
    await expect(page.getByText("Household and price settings need the API")).toBeVisible();
    await expect(page.getByRole("button", { name: "Add price" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Save household" })).toHaveCount(0);
  });

  test("still shows the API connection check and what is not built yet", async ({ page }) => {
    await page.goto("/app/settings");

    await expect(page.getByRole("heading", { name: "API connection" })).toBeVisible();
    await expect(page.getByText(/No API address is set/)).toBeVisible();
    await expect(page.getByText("Delivery days and pauses aren't built yet")).toBeVisible();
  });
});

test("the demo screens that are not built on real data say so", async ({ page }) => {
  await page.goto("/app/today");
  await expect(page.getByText("Demo data", { exact: true })).toBeVisible();
  await expect(page.getByText(/Sample household/)).toBeVisible();

  await page.goto("/app/history");
  await expect(page.getByText("History isn't built yet")).toBeVisible();

  await page.goto("/app/month");
  await expect(page.getByText("The monthly view isn't built yet")).toBeVisible();
});
