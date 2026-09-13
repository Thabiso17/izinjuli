import { defineConfig, devices } from '@playwright/test';

/**
 * Browser tests against the real application: the Angular app served from this branch,
 * talking to a locally running API and a real database.
 *
 * These cover the one thing neither the handler tests nor the API tests can see — whether the
 * screens actually work. The bug that started this round of work was a 500 on the admin players
 * page, and the symptom the user saw was not the 500 itself but a screen that refused to say
 * what had gone wrong. Only a browser can check that.
 *
 * The API is started by CI rather than by webServer here, because it needs a database behind it.
 * Point E2E_BASE_URL somewhere else to run these against a deployed environment.
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  // A failing assertion here usually means a real defect, so let it fail rather than
  // retrying until it passes.
  retries: process.env.CI ? 1 : 0,
  // The admin journeys create data, and jersey numbers are unique per team.
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: process.env.CI
    ? [['html', { outputFolder: 'playwright-report', open: 'never' }], ['list']]
    : [['list']],
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: process.env.E2E_BASE_URL
    ? undefined
    : {
        command: 'npx ng serve --configuration e2e --port 4200',
        url: 'http://localhost:4200',
        reuseExistingServer: !process.env.CI,
        timeout: 180_000,
      },
});
