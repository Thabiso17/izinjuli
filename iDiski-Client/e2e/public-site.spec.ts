import { test, expect } from '@playwright/test';

/**
 * The site a visitor sees, with no account at all. Locking down the write endpoints was easy
 * to get wrong in the other direction — an [Authorize] on a controller that also serves a
 * public list would blank a section of the homepage, and nothing below the browser would
 * notice, because the API would answer 401 perfectly correctly.
 */
test.describe('the public site', () => {
  test('the homepage renders without an account', async ({ page }) => {
    const failedRequests: string[] = [];
    page.on('response', (response) => {
      if (response.status() === 401 || response.status() === 403) {
        failedRequests.push(`${response.status()} ${response.url()}`);
      }
    });

    await page.goto('/');

    await expect(page.locator('body')).toBeVisible();
    expect(
      failedRequests,
      'a public page asking for something it is not allowed to see means a section is now blank',
    ).toEqual([]);
  });

  test.describe.configure({ mode: 'serial' });

  for (const path of ['/divisions', '/teams', '/standings', '/matches']) {
    test(`${path} is reachable and renders`, async ({ page }) => {
      const response = await page.goto(path);

      expect(response?.status()).toBeLessThan(400);
      await expect(page.locator('body')).toBeVisible();
      // A crashed Angular route leaves an empty shell behind.
      await expect(page.locator('router-outlet')).toHaveCount(1);
    });
  }

  test('a visitor is sent to the login page when they try to reach the admin area', async ({
    page,
  }) => {
    await page.goto('/admin/players');

    await expect(page).toHaveURL(/\/login/);
  });

  test('a division can be opened from the divisions list', async ({ page }) => {
    await page.goto('/divisions');

    const firstDivision = page.locator('a[href^="/divisions/"]').first();

    // The list is empty on a database with no divisions, which is a legitimate state for a
    // fresh environment rather than a failure.
    if ((await firstDivision.count()) === 0) {
      test.skip(true, 'no divisions seeded in this environment');
    }

    await firstDivision.click();
    await expect(page).toHaveURL(/\/divisions\/[0-9a-f-]+/i);
  });
});
