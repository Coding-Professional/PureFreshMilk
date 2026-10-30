import { defineConfig, devices } from "@playwright/test";

const port = 3100;
const baseURL = `http://localhost:${port}`;
const isCI = Boolean(process.env.CI);

/**
 * Browser checks against a production build: every route renders, there is no
 * horizontal overflow at 360px, and axe finds no serious accessibility issues.
 * In CI the build already exists; locally the web server command builds first.
 */
export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: true,
  forbidOnly: isCI,
  retries: 0,
  reporter: isCI ? [["list"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL,
    trace: "retain-on-failure",
    // The landing-page measure animates on load; check the settled state, not a mid-animation frame.
    contextOptions: { reducedMotion: "reduce" },
  },
  projects: [
    {
      name: "mobile-360",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 360, height: 780 },
        isMobile: true,
        hasTouch: true,
      },
    },
    {
      name: "desktop",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1280, height: 800 },
      },
    },
  ],
  webServer: {
    command: isCI
      ? `npm run start -- --port ${port}`
      : `npm run build && npm run start -- --port ${port}`,
    // These tests cover the app with no API address, where the Recipients and Settings screens say so
    // and show labelled demo data. Pin it, so a developer's .env.local cannot change what is tested.
    // (CI builds earlier, in a clean checkout.)
    env: { NEXT_PUBLIC_API_BASE_URL: "" },
    url: baseURL,
    reuseExistingServer: !isCI,
    timeout: 240_000,
  },
});
