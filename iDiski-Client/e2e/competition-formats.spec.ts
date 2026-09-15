import { test, expect, type Locator, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * Running competitions inside a division.
 *
 * A division used to be the competition, so it could run exactly one thing per season. It is
 * now a pool of teams, and what they play is a competition: a league all season, a cup for the
 * top eight, a sponsor's tournament that invites clubs from elsewhere — side by side.
 *
 * These drive the path an organiser actually takes: make the division, add the clubs, start a
 * competition, choose who is in it, draw it up, and find it on the public page.
 */
test.describe('competitions', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
  });

  test('a competition can be started in a division and appears on its page', async ({ page }) => {
    const division = await createDivision(page);
    await createTeam(page, division, `Starter A ${unique()}`);
    await createTeam(page, division, `Starter B ${unique()}`);

    const competition = await createCompetition(page, division, 'Knockout');

    // The admin list for that division shows it.
    await expect(page.locator('[data-testid="competition-row"]', { hasText: competition }))
      .toBeVisible();

    // And so does the division's public page, which is now a pool listing what it plays.
    const divisionId = await divisionIdFor(page, division);
    await page.goto(`/divisions/${divisionId}`);
    await page.waitForLoadState('networkidle');

    await expect(
      page.locator('[data-testid="competition-card"]', { hasText: competition }),
      'a division page should list the competitions being run from it',
    ).toBeVisible();
  });

  test('a knockout competition draws a bracket on its own page', async ({ page }) => {
    const division = await createDivision(page);

    for (let i = 1; i <= 4; i++) {
      await createTeam(page, division, `Entrant ${i} ${unique()}`);
    }

    const competition = await createCompetition(page, division, 'Knockout');

    const body = await generate(page, competition, { daysBetween: '0' });

    // Four entrants make two semi-finals and a final. A bracket of four is never anything
    // else, so this is exact rather than "more than nothing".
    expect(body.fixturesGenerated, 'four entrants make two semi-finals and a final').toBe(3);

    await openCompetition(page, division, competition);

    const bracket = page.locator('[data-testid="bracket"]');
    await expect(bracket).toBeVisible();
    await expect(bracket.locator('[data-testid="bracket-tie"]')).toHaveCount(3);
    await expect(bracket).toContainText('To be decided');
  });

  test('a group competition draws its groups', async ({ page }) => {
    const division = await createDivision(page);

    for (let i = 1; i <= 8; i++) {
      await createTeam(page, division, `Grouped ${i} ${unique()}`);
    }

    const competition = await createCompetition(page, division, 'GroupAndKnockout');

    const body = await generate(page, competition, { singleRound: true, groups: '2', advancing: '2' });

    // Two groups of four played once through is six ties a group, and four qualifiers make two
    // semi-finals and a final.
    expect(body.fixturesGenerated, 'twelve group ties and three bracket ties').toBe(15);

    await openCompetition(page, division, competition);

    const groups = page.locator('[data-testid="group-heading"]');
    await expect(groups).toHaveCount(2);
    await expect(groups.first()).toContainText('Group A');
    await expect(page.locator('[data-testid="bracket"]')).toBeVisible();
  });

  test('a cup of some of the division draws only those clubs', async ({ page }) => {
    // The thing the old model could not express at all: a division of six running a cup for
    // four of them.
    const division = await createDivision(page);
    const names: string[] = [];

    for (let i = 1; i <= 6; i++) {
      const team = `Squad ${i} ${unique()}`;
      names.push(team);
      await createTeam(page, division, team);
    }

    // Started with nobody in it, then four entered by hand.
    const competition = await createCompetition(page, division, 'Knockout', { enterAll: false });

    await openEntrants(page, competition);

    for (const team of names.slice(0, 4)) {
      await enterTeam(page, team);
    }

    await expect(page.locator('[data-testid="entrant-row"]')).toHaveCount(4);
    await page.locator('.modal.show button:has-text("Done")').click();

    const body = await generate(page, competition, {});

    expect(body.fixturesGenerated, 'four entrants make a bracket of three ties').toBe(3);

    await openCompetition(page, division, competition);

    // The two clubs left out are nowhere in the draw.
    const bracket = page.locator('[data-testid="bracket"]');
    await expect(bracket).toBeVisible();

    for (const excluded of names.slice(4)) {
      await expect(bracket).not.toContainText(excluded);
    }
  });

  test('two competitions run side by side in one division', async ({ page }) => {
    // The whole point: a league running all season while a cup inside it is played out.
    const division = await createDivision(page);

    for (let i = 1; i <= 4; i++) {
      await createTeam(page, division, `Dual ${i} ${unique()}`);
    }

    const league = await createCompetition(page, division, 'League');
    const cup = await createCompetition(page, division, 'Knockout');

    await generate(page, league, { singleRound: true });
    await generate(page, cup, { daysBetween: '0' });

    const divisionId = await divisionIdFor(page, division);
    await page.goto(`/divisions/${divisionId}`);
    await page.waitForLoadState('networkidle');

    const cards = page.locator('[data-testid="competition-card"]');
    await expect(cards).toHaveCount(2);
    await expect(cards.filter({ hasText: league })).toBeVisible();
    await expect(cards.filter({ hasText: cup })).toBeVisible();
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

/**
 * Picks an option by name, whatever the page has wrapped around it — several of these selects
 * append a season or a division, so an exact label match finds nothing.
 */
async function selectByName(select: Locator, name: string) {
  const labels = (await select.locator('option').allInnerTexts()).map((t) => t.trim());
  const index = labels.findIndex((label) => label.includes(name));

  expect(index, `no option named ${name}`).toBeGreaterThan(-1);

  await select.selectOption({ index });
}

/** A division is now just a pool of teams: no format, nothing to play until one is added. */
async function createDivision(page: Page): Promise<string> {
  const name = `Division ${unique()}`;

  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="add-division"]').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(name);
  await modal.locator('input[name="shortCode"]').fill(shortCode('D'));
  await modal.locator('input[name="season"]').fill('2033');

  await modal.locator('[data-testid="save-division"]').click();
  await expect(modal).toBeHidden();

  return name;
}

async function divisionIdFor(page: Page, divisionName: string): Promise<string> {
  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  const link = page
    .locator('tr', { hasText: divisionName })
    .locator('[data-testid="manage-competitions"]');

  const href = await link.getAttribute('href');
  const id = href?.split('/').filter(Boolean).at(-2);

  expect(id, `could not find the id of ${divisionName}`).toBeTruthy();
  return id!;
}

/** Opens the competitions screen for a division and leaves the page there. */
async function openCompetitions(page: Page, divisionName: string) {
  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  await page
    .locator('tr', { hasText: divisionName })
    .locator('[data-testid="manage-competitions"]')
    .click();

  await page.waitForLoadState('networkidle');
}

async function createCompetition(
  page: Page,
  divisionName: string,
  format: string,
  options: { enterAll?: boolean } = {},
): Promise<string> {
  const name = `${format} ${unique()}`;

  await openCompetitions(page, divisionName);
  await page.locator('[data-testid="add-competition"]').click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(name);
  await modal.locator('input[name="shortCode"]').fill(shortCode('C'));
  await modal.locator('[data-testid="competition-format"]').selectOption(format);

  if (options.enterAll === false) {
    await modal.locator('[data-testid="enter-all"]').uncheck();
  }

  await modal.locator('[data-testid="save-competition"]').click();
  await expect(modal).toBeHidden();

  return name;
}

async function openEntrants(page: Page, competitionName: string) {
  await page
    .locator('[data-testid="competition-row"]', { hasText: competitionName })
    .locator('[data-testid="manage-entrants"]')
    .click();

  await expect(page.locator('.modal.show')).toBeVisible();
}

async function enterTeam(page: Page, teamName: string) {
  const modal = page.locator('.modal.show');

  await selectByName(modal.locator('select[name="teamToAdd"]'), teamName);
  await modal.locator('[data-testid="enter-team"]').click();

  await expect(
    modal.locator('[data-testid="entrant-row"]', { hasText: teamName }),
  ).toBeVisible();
}

async function createTeam(page: Page, divisionName: string, teamName: string) {
  await page.goto('/admin/teams');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="add-team"]').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(teamName);
  await modal.locator('input[name="shortCode"]').fill(shortCode('T'));
  await selectByName(modal.locator('select[name="divisionId"]'), divisionName);
  await modal.locator('input[name="founded"]').fill('2020');

  await modal.locator('[data-testid="save-team"]').click();
  await expect(modal).toBeHidden();
}

/**
 * Draws a competition up. No season to fill in: it belongs to the competition, so it cannot
 * disagree with it.
 */
async function generate(
  page: Page,
  competitionName: string,
  options: { singleRound?: boolean; groups?: string; advancing?: string; daysBetween?: string },
) {
  await page.goto('/admin/matches');
  await page.waitForLoadState('networkidle');
  await page.locator('button:has-text("Generate Fixtures")').first().click();

  const modal = page.locator('.modal.show');
  await selectByName(modal.locator('select[name="competitionId"]'), competitionName);

  await modal.locator('input[name="startDate"]').fill('2033-06-01');

  if (options.daysBetween) {
    await modal.locator('input[name="daysBetweenMatchweeks"]').fill(options.daysBetween);
  }

  if (options.singleRound) {
    // The radio is a visually hidden Bootstrap btn-check, so the label is the clickable part.
    await modal.locator('label[for="singleRound"]').click();
  }

  if (options.groups) await modal.locator('[data-testid="group-count"]').fill(options.groups);
  if (options.advancing) {
    await modal.locator('[data-testid="teams-advancing"]').fill(options.advancing);
  }

  const generated = page.waitForResponse(
    (r) => r.url().includes('/api/matchresults/generate') && r.request().method() === 'POST',
  );

  await modal.locator('button:has-text("Generate Fixtures")').last().click();

  const response = await generated;
  expect(response.status(), await response.text()).toBe(200);

  return response.json();
}

/** Opens a competition's public page through its division, the way a visitor would. */
async function openCompetition(page: Page, divisionName: string, competitionName: string) {
  const divisionId = await divisionIdFor(page, divisionName);

  await page.goto(`/divisions/${divisionId}`);
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="competition-card"]', { hasText: competitionName }).click();
  await page.waitForLoadState('networkidle');

  await expect(page.locator('[data-testid="competition-name"]')).toContainText(competitionName);
}
