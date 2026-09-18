import { test, expect, type Browser, type Page } from '@playwright/test';
import { accounts, signIn } from './helpers';

/**
 * What an administrator sees, as opposed to what they may do.
 *
 * The writes were guarded all along — the API refuses a save on somebody else's club. The
 * screens were not: both admin lists call the same endpoints the public site uses, so a team
 * admin looking after one club opened Teams Management and scrolled every club in the league,
 * each with an Edit button that led to a 403.
 *
 * A role says what kind of administrator somebody is, never which competitions they
 * administer, so the check is against their actual assignments.
 *
 * Each role gets its own browser context: /login sits behind noAuthGuard, so a page that is
 * already signed in cannot get back to the form to sign in as somebody else.
 */
test.describe('admin scope', () => {
  test('a team admin sees only the clubs they administer', async ({ browser, baseURL }) => {
    const everything = await countIn(browser, baseURL, accounts.superAdmin, '/admin/teams', TEAM_CARD);
    const theirs = await countIn(browser, baseURL, accounts.teamAdmin, '/admin/teams', TEAM_CARD);

    expect(theirs, 'a team admin must still see the club they were given').toBeGreaterThan(0);
    expect(
      theirs,
      'a team admin was seeing every club in the league, not the ones they administer',
    ).toBeLessThan(everything);
  });

  test('a competition admin sees only the competitions they administer', async ({
    browser,
    baseURL,
  }) => {
    // Their assignment is a list of competitions, so this is the screen it has to narrow.
    // Divisions are no longer assigned to anybody and are not what this role runs.
    const everything = await countIn(
      browser,
      baseURL,
      accounts.superAdmin,
      '/admin/competitions',
      COMPETITION_ROW,
    );
    const theirs = await countIn(
      browser,
      baseURL,
      accounts.competitionAdmin,
      '/admin/competitions',
      COMPETITION_ROW,
    );

    expect(theirs, 'a competition admin must still see the one they were given')
      .toBeGreaterThan(0);
    expect(
      theirs,
      'a competition admin was seeing every competition being played, not the ones they run',
    ).toBeLessThan(everything);
  });

  test('a team admin is not offered the things the API would refuse', async ({
    browser,
    baseURL,
  }) => {
    await asRole(browser, baseURL, accounts.teamAdmin, async (page) => {
      await page.goto('/admin/teams');
      await page.waitForLoadState('networkidle');

      // Creating and deleting a club are division-admin and above, so offering either here
      // could only ever end in a refusal.
      await expect(page.locator('[data-testid="add-team"]')).toHaveCount(0);

      // What they can do, they are still offered — this is scoping, not a lockout.
      await expect(page.locator(TEAM_CARD).first()).toBeVisible();
      await expect(page.locator('button:has-text("Edit")').first()).toBeVisible();
    });
  });

  test('a competition admin is offered neither a new division nor a new competition', async ({
    browser,
    baseURL,
  }) => {
    await asRole(browser, baseURL, accounts.competitionAdmin, async (page) => {
      await page.goto('/admin/divisions');
      await page.waitForLoadState('networkidle');

      // Creating, editing and deleting a division are all super-admin only at the API, so for
      // a competition admin this page is a read-only look at where the clubs come from.
      await expect(page.locator('[data-testid="add-division"]')).toHaveCount(0);
      await expect(page.locator(DIVISION_ROW).first()).toBeVisible();

      await page.goto('/admin/competitions');
      await page.waitForLoadState('networkidle');

      // Starting one is the organiser's: otherwise the role could hand itself a competition
      // and then administer it.
      await expect(page.locator('[data-testid="add-competition"]')).toHaveCount(0);
      await expect(page.locator(COMPETITION_ROW).first()).toBeVisible();
    });
  });

  test('a super admin still sees the whole league', async ({ browser, baseURL }) => {
    // The half that stops this being a change that simply hides things from everybody.
    await asRole(browser, baseURL, accounts.superAdmin, async (page) => {
      await page.goto('/admin/divisions');
      await page.waitForLoadState('networkidle');

      await expect(page.locator('[data-testid="add-division"]').first()).toBeVisible();

      await page.goto('/admin/teams');
      await page.waitForLoadState('networkidle');

      await expect(page.locator('[data-testid="add-team"]').first()).toBeVisible();
    });
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

/** A team is a card on the teams board; a division is a row in the divisions table. */
const TEAM_CARD = '[data-testid="team-card"]';
const DIVISION_ROW = '[data-testid="division-competitions-cell"]';
const COMPETITION_ROW = '[data-testid="competition-row"]';

async function asRole(
  browser: Browser,
  baseURL: string | undefined,
  account: { email: string; password: string },
  work: (page: Page) => Promise<void>,
) {
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();

  try {
    await signIn(page, account);
    await expect(page).toHaveURL(/\/admin/, { timeout: 20_000 });
    await work(page);
  } finally {
    await context.close();
  }
}

async function countIn(
  browser: Browser,
  baseURL: string | undefined,
  account: { email: string; password: string },
  path: string,
  selector: string,
): Promise<number> {
  let count = 0;

  await asRole(browser, baseURL, account, async (page) => {
    await page.goto(path);
    await page.waitForLoadState('networkidle');

    // Wait for the first row before counting: an empty list and a list that has not loaded
    // look identical, and counting too early would make every one of these pass.
    await expect(page.locator(selector).first()).toBeVisible({ timeout: 20_000 });

    count = await page.locator(selector).count();
  });

  return count;
}
