import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Setting a league up through the admin screens, the way an organiser does.
 *
 * Shared rather than per-spec because the order of these steps is the thing under test in more
 * than one file: a division holds the clubs, a competition says what they play, and the draw
 * comes last. A spec that wants a drawn-up competition to act on should not have to rebuild
 * that path, and two copies of it drift.
 */

export function unique() {
  return `${Date.now()}${Math.floor(Math.random() * 1000)}`.slice(-8);
}

/** Short codes are unique per season and must be uppercase letters or digits. */
export function shortCode(prefix: string) {
  return `${prefix}${unique()}`.toUpperCase().slice(0, 12);
}

/**
 * Picks an option by name, whatever the page has wrapped around it — several of these selects
 * append a season or a division, so an exact label match finds nothing.
 */
export async function selectByName(select: Locator, name: string) {
  const labels = (await select.locator('option').allInnerTexts()).map((t) => t.trim());
  const index = labels.findIndex((label) => label.includes(name));

  expect(index, `no option named ${name}`).toBeGreaterThan(-1);

  await select.selectOption({ index });
}

/** A division is now just a pool of teams: no format, nothing to play until one is added. */
export async function createDivision(page: Page): Promise<string> {
  const name = `Division ${unique()}`;

  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="add-division"]').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(name);
  await modal.locator('input[name="shortCode"]').fill(shortCode('D'));
  await modal.locator('input[name="season"]').fill('2033');

  // Asked for, not optional: it is what decides which competitions these clubs may enter, and
  // a division that leaves it blank is a division whose clubs nobody can place with certainty.
  await modal.locator('[data-testid="division-gender"]').selectOption('Male');

  await modal.locator('[data-testid="save-division"]').click();
  await expect(modal).toBeHidden();

  return name;
}

/**
 * A division's id, read off the edit button on its row. It used to come from the competitions
 * link, which no longer points at one division — a division does not have competitions.
 */
export async function divisionIdFor(page: Page, divisionName: string): Promise<string> {
  await page.goto('/admin/divisions');
  await page.waitForLoadState('networkidle');

  const row = page.locator('tr', { hasText: divisionName });
  const id = await row.getAttribute('data-division-id');

  expect(id, `could not find the id of ${divisionName}`).toBeTruthy();
  return id!;
}

/** The competitions screen. Not a division's — there is only one, from the admin menu. */
export async function openCompetitions(page: Page) {
  await page.goto('/admin/competitions');
  await page.waitForLoadState('networkidle');
}

/**
 * Starts a competition. It belongs to no division; passing one only fills the entry list with
 * that division's clubs straight away, which is how a league gets set up.
 */
export async function createCompetition(
  page: Page,
  divisionName: string | null,
  format: string,
): Promise<string> {
  const name = `${format} ${unique()}`;

  await openCompetitions(page);
  await page.locator('[data-testid="add-competition"]').click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  await modal.locator('input[name="name"]').fill(name);
  await modal.locator('input[name="shortCode"]').fill(shortCode('C'));
  await modal.locator('[data-testid="competition-format"]').selectOption(format);

  if (divisionName) {
    await selectByName(modal.locator('[data-testid="enter-teams-from"]'), divisionName);
  }

  await modal.locator('[data-testid="save-competition"]').click();
  await expect(modal).toBeHidden();

  return name;
}

export async function openEntrants(page: Page, competitionName: string) {
  await page
    .locator('[data-testid="competition-row"]', { hasText: competitionName })
    .locator('[data-testid="manage-entrants"]')
    .click();

  await expect(page.locator('.modal.show')).toBeVisible();
}

export async function enterTeam(page: Page, teamName: string) {
  const modal = page.locator('.modal.show');

  await selectByName(modal.locator('select[name="teamToAdd"]'), teamName);
  await modal.locator('[data-testid="enter-team"]').click();

  await expect(
    modal.locator('[data-testid="entrant-row"]', { hasText: teamName }),
  ).toBeVisible();
}

export async function createTeam(page: Page, divisionName: string, teamName: string) {
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
export async function generate(
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
export async function openCompetition(page: Page, divisionName: string, competitionName: string) {
  const divisionId = await divisionIdFor(page, divisionName);

  await page.goto(`/divisions/${divisionId}`);
  await page.waitForLoadState('networkidle');

  await page.locator('[data-testid="competition-card"]', { hasText: competitionName }).click();
  await page.waitForLoadState('networkidle');

  await expect(page.locator('[data-testid="competition-name"]')).toContainText(competitionName);
}
