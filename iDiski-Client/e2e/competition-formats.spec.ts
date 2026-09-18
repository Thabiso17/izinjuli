import { test, expect } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';
import {
  createCompetition,
  createDivision,
  createTeam,
  divisionIdFor,
  enterTeam,
  generate,
  openCompetition,
  openEntrants,
  shortCode,
  unique,
} from './admin-setup';

/**
 * Running a competition, and the division whose clubs play in it.
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

  test('a competition can be started from the admin menu', async ({ page }) => {
    // How an organiser actually looks for it. This screen existed behind a division's row
    // long before it had a menu entry, which meant it may as well not have existed.
    const division = await createDivision(page);
    await createTeam(page, division, `Menu A ${unique()}`);
    await createTeam(page, division, `Menu B ${unique()}`);

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.locator('.nav-link.dropdown-toggle', { hasText: 'Admin' }).click();

    const entry = page.locator('[data-testid="nav-competitions"]');
    await expect(entry, 'competitions needs a way in that is not a division row').toBeVisible();

    await entry.click();
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/admin\/competitions/);

    const name = `Menu Cup ${unique()}`;

    await page.locator('[data-testid="add-competition"]').click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('input[name="name"]').fill(name);
    await modal.locator('input[name="shortCode"]').fill(shortCode('M'));
    await modal.locator('[data-testid="competition-format"]').selectOption('Knockout');
    await modal.locator('[data-testid="competition-max-teams"]').fill('8');

    // Nobody entered yet: a cup starts empty and the organiser chooses the field. The
    // division only exists here to have clubs available to choose from later.
    void division;

    await modal.locator('[data-testid="save-competition"]').click();
    await expect(modal).toBeHidden();

    const row = page.locator('[data-testid="competition-row"]', { hasText: name });
    await expect(row).toBeVisible();

    // Eight places and nobody in them yet, which is what a cup looks like before the draw.
    await expect(row.locator('[data-testid="entrant-progress"]')).toContainText('0 of 8 entered');
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
    const competition = await createCompetition(page, null, 'Knockout');

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
