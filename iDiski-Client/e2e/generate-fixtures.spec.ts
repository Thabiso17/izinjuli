import { test, expect, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';
import { createCompetition, createDivision, createTeam, generate, unique } from './admin-setup';

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
  });

  test('a competition that has already been drawn up refuses a second draw', async ({ page }) => {
    // Drawn up here rather than borrowed from the seeded league: every spec in this suite
    // leaves competitions behind, so "the first one in the dropdown" stopped meaning "one that
    // has fixtures" and the guard was being asked about a competition with no entrants.
    const division = await createDivision(page);
    await createTeam(page, division, `Drawn A ${unique()}`);
    await createTeam(page, division, `Drawn B ${unique()}`);

    const competition = await createCompetition(page, division, 'League');

    const first = await generate(page, competition, { singleRound: true });
    expect(first.fixturesGenerated, 'two clubs played once through is one fixture').toBe(1);

    await drawAgain(page, competition);

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
    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');

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
 * The same click as the first draw, without expecting it to succeed — which is the whole
 * point, so this cannot go through the shared helper.
 */
async function drawAgain(page: Page, competitionName: string) {
  await page.goto('/admin/matches');
  await page.waitForLoadState('networkidle');
  await page.locator('button:has-text("Generate Fixtures")').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  const select = modal.locator('select[name="competitionId"]');
  const labels = (await select.locator('option').allInnerTexts()).map((t) => t.trim());
  const index = labels.findIndex((label) => label.includes(competitionName));

  expect(index, `${competitionName} is not in the dropdown`).toBeGreaterThan(-1);

  await select.selectOption({ index });
  await modal.locator('input[name="startDate"]').fill('2033-06-01');
  await modal.locator('label[for="singleRound"]').click();
  await modal.locator('button:has-text("Generate Fixtures")').last().click();
}
