import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

const routes = [
  { path: "/", heading: "Every morning's milk, measured to the millilitre." },
  { path: "/login", heading: "Log in" },
  { path: "/signup", heading: "Create an account" },
  { path: "/app/today", heading: "Today" },
  { path: "/app/month", heading: "This month" },
  { path: "/app/history", heading: "History" },
  { path: "/app/recipients", heading: "Recipients" },
  { path: "/app/settings", heading: "Settings" },
];

for (const route of routes) {
  test.describe(`route ${route.path}`, () => {
    test.beforeEach(async ({ page }) => {
      await page.goto(route.path);
      await expect(page.getByRole("heading", { level: 1, name: route.heading })).toBeVisible();
    });

    test("does not scroll horizontally", async ({ page }) => {
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );
      expect(overflow).toBe(0);
    });

    test("has no serious accessibility violations", async ({ page }) => {
      const results = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
        .analyze();
      const serious = results.violations
        .filter((violation) => violation.impact === "serious" || violation.impact === "critical")
        .map((violation) => `${violation.id}: ${violation.help} (${violation.nodes.length})`);

      expect(serious).toEqual([]);
    });
  });
}

test("/app redirects to Today", async ({ page }) => {
  await page.goto("/app");
  await expect(page).toHaveURL(/\/app\/today$/);
});

test("unknown pages show the not-found page", async ({ page }) => {
  const response = await page.goto("/no-such-page");
  expect(response?.status()).toBe(404);
  await expect(
    page.getByRole("heading", { level: 1, name: "This page doesn't exist" }),
  ).toBeVisible();
});
