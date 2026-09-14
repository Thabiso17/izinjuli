import { test, expect, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * Generating a season's fixtures — the one admin action that writes dozens of rows from a
 * single click, and the one that had no test of any kind.
 *
 * A round-robin that is subtly wrong does not fail loudly. It produces a season that looks
 * plausible, and by the time anyone notices, results have been entered against it. The
 * arithmetic is pinned in GenerateFixturesTests against the database; this checks the screen
 * actually drives it and reports back.
 */
test.describe('generating fixtures', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');
  });

  test('a division with enough teams gets a full round of fixtures', async ({ page }) => {
    const division = await divisionWithAtLeastTwoTeams(page);
    if (!division) test.skip(true, 'no division in this environment has two teams');

    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('select[name="divisionId"]').selectOption({ label: division! });
    await modal.locator('input[name="startDate"]').fill('2027-03-01');

    const generated = page.waitForResponse(
      (r) =>
        r.url().includes('/api/matchresults/generate') && r.request().method() === 'POST',
    );

    await modal.locator('button:has-text("Generate Fixtures")').last().click();

    const response = await generated;
    expect(response.status(), await response.text()).toBe(200);

    // The page reports what it created; a silent success is indistinguishable from nothing
    // having happened.
    const success = page.locator('.alert-success');
    await expect(success).toBeVisible();
    await expect(success).toContainText(/Generated \d+ fixtures/i);

    const body = await response.json();
    expect(body.fixturesGenerated, 'a round-robin of two or more clubs is at least one fixture')
      .toBeGreaterThan(0);
    expect(body.matchweeksCreated).toBeGreaterThan(0);
  });

  test('a division without enough teams is refused, and says why', async ({ page }) => {
    const division = await divisionWithFewerThanTwoTeams(page);
    if (!division) test.skip(true, 'every division in this environment has two teams');

    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('select[name="divisionId"]').selectOption({ label: division! });
    await modal.locator('input[name="startDate"]').fill('2027-03-01');
    await modal.locator('button:has-text("Generate Fixtures")').last().click();

    const failure = page.locator('.alert-danger');
    await expect(failure).toBeVisible();
    await expect(
      failure,
      'the admin needs to be told it is the squad list at fault, not a server error',
    ).toContainText(/team/i);
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

/**
 * Divisions are read from the filter bar, whose team dropdown is the page's own account of
 * which clubs play where — so the test picks a division that can actually produce a fixture
 * rather than assuming how the data is seeded.
 */
async function divisionTeamCounts(page: Page): Promise<Map<string, number>> {
  const filters = page.locator('.card.mb-4').first();
  const division = filters.locator('select').first();
  const team = filters.locator('select').nth(1);

  const names = (await division.locator('option').allInnerTexts()).map((n) => n.trim());
  const counts = new Map<string, number>();

  // Index 0 is "All Divisions".
  for (let i = 1; i < names.length; i++) {
    await division.selectOption({ index: i });
    await page.waitForLoadState('networkidle');
    // Minus one for the "All Teams" placeholder.
    counts.set(names[i], (await team.locator('option').count()) - 1);
  }

  await division.selectOption({ index: 0 });
  await page.waitForLoadState('networkidle');

  return counts;
}

async function divisionWithAtLeastTwoTeams(page: Page): Promise<string | null> {
  for (const [name, count] of await divisionTeamCounts(page)) {
    if (count >= 2) return name;
  }
  return null;
}

async function divisionWithFewerThanTwoTeams(page: Page): Promise<string | null> {
  for (const [name, count] of await divisionTeamCounts(page)) {
    if (count < 2) return name;
  }
  return null;
}
