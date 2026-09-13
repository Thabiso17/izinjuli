import { test, expect, type Page } from '@playwright/test';

/**
 * The site a visitor sees, with no account at all.
 *
 * Locking down the write endpoints was easy to get wrong in the other direction: an [Authorize]
 * on a controller that also serves a public list would blank a section of the homepage, and
 * nothing below the browser would notice, because the API would answer 401 perfectly correctly.
 * So these watch what the page asks for as well as what it renders.
 */
test.describe('the public site', () => {
  test('the homepage renders without an account', async ({ page }) => {
    const refused: string[] = [];
    page.on('response', (response) => {
      if ([401, 403].includes(response.status())) {
        refused.push(`${response.status()} ${response.url()}`);
      }
    });

    const crashes = collectPageErrors(page);

    await page.goto('/');

    await expect(page.locator('app-root')).not.toBeEmpty();
    expect(
      refused,
      'a public page asking for something it is not allowed to see means a section is now blank',
    ).toEqual([]);
    expect(crashes).toEqual([]);
  });

  for (const path of ['/divisions', '/teams', '/standings', '/matches']) {
    test(`${path} renders`, async ({ page }) => {
      const crashes = collectPageErrors(page);

      const response = await page.goto(path);

      expect(response?.status()).toBeLessThan(400);
      // A route that throws while rendering still leaves the shell behind, so check that
      // something was actually put inside it.
      await expect(page.locator('app-root')).not.toBeEmpty();
      expect(crashes, `${path} threw while rendering`).toEqual([]);
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

    // An empty list is a legitimate state for a fresh environment rather than a failure.
    if ((await firstDivision.count()) === 0) {
      test.skip(true, 'no divisions seeded in this environment');
    }

    await firstDivision.click();
    await expect(page).toHaveURL(/\/divisions\/[0-9a-f-]+/i);
  });
});

/** Uncaught exceptions from the page, which is what a route that fails to render looks like. */
function collectPageErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  return errors;
}
