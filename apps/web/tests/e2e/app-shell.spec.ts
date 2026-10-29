import { expect, test } from "@playwright/test";

const isPhone = (projectName: string) => projectName === "mobile-360";

test("phones get a three-tab bar; larger screens get the full sidebar", async ({
  page,
}, testInfo) => {
  await page.goto("/app/today");

  // Only the navigation for the current breakpoint is visible (and in the accessibility tree).
  const mainNav = page.getByRole("navigation", { name: "Main" });
  await expect(mainNav).toHaveCount(1);

  const expectedLinks = isPhone(testInfo.project.name)
    ? ["Today", "This month", "History"]
    : ["Today", "This month", "History", "Recipients", "Settings"];
  await expect(mainNav.getByRole("link")).toHaveText(expectedLinks);
  await expect(mainNav.getByRole("link", { name: "Today" })).toHaveAttribute(
    "aria-current",
    "page",
  );

  if (isPhone(testInfo.project.name)) {
    const household = page.getByRole("navigation", { name: "Household" });
    await expect(household.getByRole("link", { name: "Recipients" })).toBeVisible();
    await expect(household.getByRole("link", { name: "Settings" })).toBeVisible();
  }
});

test("navigating marks the new page as current", async ({ page }) => {
  await page.goto("/app/today");
  await page
    .getByRole("navigation", { name: "Main" })
    .getByRole("link", { name: "This month" })
    .click();

  await expect(page).toHaveURL(/\/app\/month$/);
  await expect(
    page.getByRole("navigation", { name: "Main" }).getByRole("link", { name: "This month" }),
  ).toHaveAttribute("aria-current", "page");
});

test("controls on phones are at least 44px touch targets", async ({ page }, testInfo) => {
  test.skip(!isPhone(testInfo.project.name), "touch-target sizes are checked at phone width");
  await page.goto("/app/today");
  await page.getByRole("button", { name: "Delivered for Recipient A" }).click();

  const controls = page.locator("main button:visible, nav a:visible");
  const count = await controls.count();
  expect(count).toBeGreaterThan(5);

  for (let index = 0; index < count; index += 1) {
    const box = await controls.nth(index).boundingBox();
    const name = (await controls.nth(index).textContent())?.trim() || `control ${index}`;
    expect(box, name).not.toBeNull();
    expect(Math.round(box!.width), `${name} width`).toBeGreaterThanOrEqual(44);
    expect(Math.round(box!.height), `${name} height`).toBeGreaterThanOrEqual(44);
  }
});
