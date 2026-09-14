import { test, expect, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * Choosing how a competition is played, and generating it.
 *
 * The bracket builder and the group generator are pinned against the database in
 * TournamentGenerationTests. None of that was reachable from the app: a division had no way
 * to say it was anything but a league, so the code could not be run by the person it was
 * written for. These check the path exists from end to end — pick the format, add the
 * entrants, generate, and get a bracket back.
 */
test.describe('competition formats', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
  });

  test('a knockout division can be created and says so in the list', async ({ page }) => {
    const name = await createDivision(page, 'Knockout');

    const row = page.locator('tr', { hasText: name });
    await expect(row).toBeVisible();
    await expect(
      row.locator('[data-testid="division-format-cell"]'),
      'the list has to say what kind of competition each one is',
    ).toContainText('Knockout');
  });

  test('reopening a division shows the format it was saved with', async ({ page }) => {
    const name = await createDivision(page, 'GroupAndKnockout');

    await page.locator('tr', { hasText: name }).locator('button').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();
    await expect(modal.locator('[data-testid="division-format"]')).toHaveValue(
      'GroupAndKnockout',
    );
  });

  test('the generate dialog describes the format it is about to produce', async ({ page }) => {
    const name = await createDivision(page, 'Knockout');

    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');
    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();
    await modal.locator('select[name="divisionId"]').selectOption({ label: name });

    // The dialog used to promise a round-robin whatever it was about to build.
    await expect(modal.locator('[data-testid="generate-explains-format"]')).toContainText(
      /bracket/i,
    );

    // Home and away is meaningless in a knockout — a tie is played once.
    await expect(modal.locator('input[name="isHomeAndAway"]')).toHaveCount(0);
    await expect(modal.locator('[data-testid="group-count"]')).toHaveCount(0);
  });

  test('a group competition asks how many groups, and a league does not', async ({ page }) => {
    const groups = await createDivision(page, 'GroupAndKnockout');

    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');
    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await modal.locator('select[name="divisionId"]').selectOption({ label: groups });

    await expect(modal.locator('[data-testid="group-count"]')).toBeVisible();
    await expect(modal.locator('[data-testid="teams-advancing"]')).toBeVisible();

    // Switching to a league puts the dialog back, rather than leaving group questions on a
    // competition that has no groups.
    const divisions = modal.locator('select[name="divisionId"] option');
    const league = (await divisions.allInnerTexts())
      .map((t) => t.trim())
      .find((t) => t && t !== 'Select Division' && t !== groups);

    if (league) {
      await modal.locator('select[name="divisionId"]').selectOption({ label: league });
      await expect(modal.locator('[data-testid="group-count"]')).toHaveCount(0);
    }
  });

  test('a knockout division with four teams generates a bracket', async ({ page }) => {
    const name = await createDivision(page, 'Knockout');

    // Four entrants: two semi-finals and a final, which is the smallest bracket with a round
    // that has to be filled in by somebody winning rather than by the draw.
    for (let i = 1; i <= 4; i++) {
      await createTeam(page, name, `Entrant ${i} ${unique()}`);
    }

    await page.goto('/admin/matches');
    await page.waitForLoadState('networkidle');
    await page.locator('button:has-text("Generate Fixtures")').first().click();

    const modal = page.locator('.modal.show');
    await modal.locator('select[name="divisionId"]').selectOption({ label: name });
    await modal.locator('input[name="season"]').fill('2033');
    await modal.locator('input[name="startDate"]').fill('2033-06-04');
    // A tournament played out over one weekend: every round on the same day.
    await modal.locator('input[name="daysBetweenMatchweeks"]').fill('0');

    const generated = page.waitForResponse(
      (r) => r.url().includes('/api/matchresults/generate') && r.request().method() === 'POST',
    );

    await modal.locator('button:has-text("Generate Fixtures")').last().click();

    const response = await generated;
    expect(response.status(), await response.text()).toBe(200);

    const body = await response.json();

    // Two semi-finals and a final. A bracket of four is never anything else, so this is an
    // exact number rather than "more than nothing".
    expect(body.fixturesGenerated, 'four entrants make two semi-finals and a final').toBe(3);
    expect(body.matchweeksCreated).toBe(2);
    expect(
      body.firstMatchDate.slice(0, 10),
      'with no days between rounds the whole thing is played on one day',
    ).toBe(body.lastMatchDate.slice(0, 10));
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

function unique() {
  return `${Date.now()}${Math.floor(Math.random() * 1000)}`.slice(-8);
}

/** Short codes are unique per season and must be uppercase letters or digits. */
function shortCode(prefix: string) {
  return `${prefix}${unique()}`.toUpperCase().slice(0, 12);
}

async function createDivision(page: Page, format: string): Promise<string> {
  const name = `${format} ${unique()}`;

  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="add-division"]').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(name);
  await modal.locator('input[name="shortCode"]').fill(shortCode('K'));
  await modal.locator('input[name="season"]').fill('2033');
  await modal.locator('[data-testid="division-format"]').selectOption(format);

  await modal.locator('[data-testid="save-division"]').click();
  await expect(modal).toBeHidden();

  return name;
}

async function createTeam(page: Page, divisionName: string, teamName: string) {
  await page.goto('/admin/teams');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="add-team"]').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(teamName);
  await modal.locator('input[name="shortCode"]').fill(shortCode('T'));
  await modal.locator('select[name="divisionId"]').selectOption({ label: divisionName });
  await modal.locator('input[name="founded"]').fill('2020');

  await modal.locator('[data-testid="save-team"]').click();
  await expect(modal).toBeHidden();
}
