import { test, expect, type Page, type Locator } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * The filters on the admin pages, driven through the browser.
 *
 * This is the gap that let five filter bugs reach the user with CI green. The suite covered
 * journeys — signing in, creating a player — but nothing that read a list and narrowed it, so a
 * filter the page never sent and a filter the API silently dropped both looked like success.
 *
 * These assert an invariant rather than a row count: everything on screen belongs to what was
 * selected. That distinction matters because of how the data happens to sit — every seeded team
 * is in one division, so "filter by that division" and "no filter at all" return the same rows,
 * and a test comparing counts would pass against the very bug it was written for. Checking
 * membership, for every division in turn, cannot pass that way.
 */
test.describe('admin filters', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
  });

  test('the players grid only shows players from the chosen division', async ({ page }) => {
    await page.goto('/admin/players');
    await settle(page);

    const filters = filterBar(page);
    const division = filters.locator('select').first();
    const team = filters.locator('select').nth(1);

    const divisionCount = await division.locator('option').count();
    let rowsSeen = 0;

    for (let i = 0; i < divisionCount; i++) {
      await division.selectOption({ index: i });
      await settle(page);

      // The team dropdown is the page's own answer to "which teams are in this division", so
      // the grid is checked against the page rather than against assumptions about the data.
      const teamsInDivision = await labels(team);
      const onScreen = await texts(page, 'player-team');
      rowsSeen += onScreen.length;

      for (const name of onScreen) {
        expect(
          teamsInDivision,
          `a player from ${name} is showing, but that team is not in the chosen division`,
        ).toContain(name);
      }
    }

    expect(rowsSeen, 'no players were listed at all, so this proved nothing').toBeGreaterThan(0);
  });

  test('clearing the team filter falls back to the division, not the whole league', async ({
    page,
  }) => {
    await page.goto('/admin/players');
    await settle(page);

    const filters = filterBar(page);
    const division = filters.locator('select').first();
    const team = filters.locator('select').nth(1);

    const index = await firstDivisionWithTeams(page, division, team);
    if (index === null) test.skip(true, 'no division in this environment has teams');

    await division.selectOption({ index: index! });
    await settle(page);

    // Narrow to a team, then put the filter back to "All Teams". The reported bug was that
    // this widened the grid to every player in the league rather than back to the division.
    await team.selectOption({ index: 1 });
    await settle(page);
    await team.selectOption({ index: 0 });
    await settle(page);

    const teamsInDivision = await labels(team);
    for (const name of await texts(page, 'player-team')) {
      expect(
        teamsInDivision,
        `${name} is not in this division, so the grid widened past it`,
      ).toContain(name);
    }
  });

  test('the matches list only shows fixtures from the chosen division', async ({ page }) => {
    await page.goto('/admin/matches');
    await settle(page);

    const division = filterBar(page).locator('select').first();
    const divisionNames = await labels(division);
    let cardsSeen = 0;

    // Index 0 is "All Divisions", which has nothing to check.
    for (let i = 1; i < divisionNames.length; i++) {
      await division.selectOption({ index: i });
      await settle(page);

      const shown = await texts(page, 'match-division');
      cardsSeen += shown.length;

      for (const name of shown) {
        expect(name).toBe(divisionNames[i]);
      }
    }

    expect(cardsSeen, 'no fixtures were listed at all, so this proved nothing').toBeGreaterThan(0);
  });

  test('the matches team filter keeps away fixtures, not just home ones', async ({ page }) => {
    await page.goto('/admin/matches');
    await settle(page);

    const filters = filterBar(page);
    const division = filters.locator('select').first();
    const team = filters.locator('select').nth(1);

    const index = await firstDivisionWithTeams(page, division, team);
    if (index === null) test.skip(true, 'no division in this environment has teams');

    await division.selectOption({ index: index! });
    await settle(page);

    const teamNames = await labels(team);
    let checkedATeamWithFixtures = false;

    for (let i = 1; i < teamNames.length; i++) {
      await team.selectOption({ index: i });
      await settle(page);

      const cards = page.locator('[data-testid="match-card"]');
      const count = await cards.count();
      if (count === 0) continue;

      checkedATeamWithFixtures = true;

      for (let card = 0; card < count; card++) {
        const home = (
          await cards.nth(card).locator('[data-testid="match-home"]').innerText()
        ).trim();
        const away = (
          await cards.nth(card).locator('[data-testid="match-away"]').innerText()
        ).trim();

        // The point of the filter: a club plays half its fixtures away, and those are still
        // its fixtures. Matching only the home side would quietly halve the season.
        expect(
          [home, away],
          `a fixture is showing that ${teamNames[i]} is not playing in`,
        ).toContain(teamNames[i]);
      }

      break;
    }

    expect(
      checkedATeamWithFixtures,
      'no team in this division had fixtures, so this proved nothing',
    ).toBe(true);
  });

  test('the fixture form only offers teams from the division chosen above it', async ({ page }) => {
    await page.goto('/admin/matches');
    await settle(page);

    await page.locator('button:has-text("Create Fixture")').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    const division = modal.locator('select[name="divisionId"]');
    const home = modal.locator('select[name="homeTeamId"]');
    const away = modal.locator('select[name="awayTeamId"]');

    const divisionNames = await labels(division);
    if (divisionNames.length < 2) test.skip(true, 'no divisions to choose from');

    const everyTeam = await labels(home);

    // Each division in turn. The pickers must never offer a team from another one, or two
    // clubs from different divisions can be scheduled against each other, and that result
    // then feeds the standings.
    let narrowedAtLeastOnce = false;

    for (let i = 1; i < divisionNames.length; i++) {
      await division.selectOption({ index: i });

      const homeOptions = await labels(home);
      const awayOptions = await labels(away);

      expect(homeOptions, 'the two pickers should offer the same teams').toEqual(awayOptions);

      if (homeOptions.length < everyTeam.length) narrowedAtLeastOnce = true;
    }

    expect(
      narrowedAtLeastOnce,
      'no division narrowed the team pickers, so they may still be listing the whole league',
    ).toBe(true);
  });

  test('the suspensions dashboard can narrow by team', async ({ page }) => {
    await page.goto('/admin/suspensions');
    await settle(page);

    const filters = filterBar(page);
    const division = filters.locator('select').first();
    const team = filters.locator('select').nth(1);

    await expect(team, 'the dashboard needs a team filter, not only a division one').toBeVisible();

    const divisionCount = await division.locator('option').count();

    for (let i = 0; i < divisionCount; i++) {
      await division.selectOption({ index: i });
      await settle(page);

      const teamsInDivision = await labels(team);
      for (const name of await texts(page, 'suspension-team')) {
        expect(teamsInDivision).toContain(name);
      }
    }
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

/** The filter card, so a modal's selects can never be mistaken for a filter. */
function filterBar(page: Page): Locator {
  return page.locator('.card.mb-4').first();
}

/** Option labels of a select, trimmed and in order. Index 0 is the "All …" placeholder. */
async function labels(select: Locator): Promise<string[]> {
  return (await select.locator('option').allInnerTexts()).map((l) => l.trim());
}

async function texts(page: Page, testId: string): Promise<string[]> {
  return (await page.locator(`[data-testid="${testId}"]`).allInnerTexts()).map((t) => t.trim());
}

/**
 * The index of the first division whose team dropdown offers a real team. A division with none
 * is a legitimate state and simply has nothing to narrow to.
 */
async function firstDivisionWithTeams(
  page: Page,
  division: Locator,
  team: Locator,
): Promise<number | null> {
  const count = await division.locator('option').count();

  for (let i = 1; i < count; i++) {
    await division.selectOption({ index: i });
    await settle(page);
    if ((await team.locator('option').count()) > 1) return i;
  }

  return null;
}

/** Let the reload triggered by a filter change finish before reading the page. */
async function settle(page: Page) {
  await page.waitForLoadState('networkidle');
}
