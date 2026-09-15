import { test, expect, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * Drawing a competition up, and refusing to draw it twice.
 *
 * Generating writes dozens of rows from a single click, and doing it twice used to leave a
 * competition holding two complete copies of itself with nothing on screen saying so. The
 * arithmetic is pinned against the database in GenerateFixturesTests and the happy path is
 * driven end to end in competition-formats.spec.ts; what is left here is the guard, which only
 * exists where somebody can click the button a second time.
 *
 * The season is no longer part of this. It belongs to the competition, so there is no year to
 * type and no year to get wrong.
 */
test.describe('generating fixtures', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');
  });

  test('a competition that has already been drawn up refuses a second draw', async ({ page }) => {
    const competition = await aSeededCompetition(page);
    if (!competition) test.skip(true, 'nothing is seeded to generate against');

    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('select[name="competitionId"]').selectOption({ index: competition!.index });
    await modal.locator('input[name="startDate"]').fill('2031-03-01');
    await modal.locator('button:has-text("Generate Fixtures")').last().click();

    const failure = page.locator('.alert-danger');
    await expect(failure).toBeVisible();

    // Said plainly, and with the count, because the alternative is an admin quietly ending up
    // with two seasons stacked on each other.
    await expect(
      failure,
      'the admin needs to be told it is already drawn, not given a server error',
    ).toContainText(/already has \d+ fixtures/i);
  });

  test('the dialog asks for a competition rather than a division', async ({ page }) => {
    // A division runs several at once, so "which division" stopped identifying anything that
    // could be drawn up.
    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await expect(modal.locator('select[name="competitionId"]')).toBeVisible();
    await expect(modal.locator('select[name="divisionId"]')).toHaveCount(0);

    // And there is no season to type: the competition carries its own.
    await expect(modal.locator('input[name="season"]')).toHaveCount(0);
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

/**
 * A competition from the seeded league, which by definition already has its fixtures — that is
 * what makes it the right subject for the double-draw guard.
 */
async function aSeededCompetition(page: Page): Promise<{ index: number } | null> {
  await page.locator('button:has-text("Generate Fixtures")').first().click();

  const modal = page.locator('.modal.show');
  const options = await modal.locator('select[name="competitionId"] option').allInnerTexts();

  await modal.locator('button:has-text("Cancel")').first().click();
  await expect(modal).toBeHidden();

  // Index 0 is the "Select Competition" placeholder.
  return options.length > 1 ? { index: 1 } : null;
}
