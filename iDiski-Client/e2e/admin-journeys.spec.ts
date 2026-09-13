import { test, expect, type Page } from '@playwright/test';
import { accounts, signIn, signInAndWaitForAdmin, someJerseyNumber } from './helpers';

test.describe.configure({ mode: 'serial' });

test.describe('signing in', () => {
  test('a super admin reaches the admin area', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
  });

  test('a wrong password is refused, and says so on the page', async ({ page }) => {
    await signIn(page, { email: accounts.superAdmin.email, password: 'not-the-password' });

    await expect(page.locator('.error-alert')).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test('a deactivated account cannot sign in', async ({ page }) => {
    await signIn(page, accounts.inactive);

    await expect(page.locator('.error-alert')).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe('the admin players page', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/players');
  });

  test('creating a player saves without a server error', async ({ page }) => {
    const jersey = someJerseyNumber();

    const response = await addPlayer(page, {
      firstName: 'Playwright',
      lastName: `Signing${Date.now()}`.slice(0, 20),
      jersey,
    });

    expect(
      response.status(),
      'this is the exact request that returned 500 from the admin page',
    ).toBeLessThan(500);
    expect(response.status()).toBeLessThan(400);
  });

  test('a rejected save explains itself instead of showing a generic error', async ({ page }) => {
    // A squad number already in use is the cleanest thing the browser cannot catch for
    // itself: the form has no way to know, so the refusal has to come back from the API and
    // be rendered on the page.
    const jersey = someJerseyNumber();
    const first = await addPlayer(page, {
      firstName: 'First',
      lastName: `Claimant${Date.now()}`.slice(0, 20),
      jersey,
    });
    expect(first.status(), 'the first player has to land for the clash to be real')
      .toBeLessThan(400);

    const second = await addPlayer(page, {
      firstName: 'Second',
      lastName: `Claimant${Date.now()}`.slice(0, 20),
      jersey,
    });
    expect(second.status()).toBe(422);

    const alert = page.locator('.alert-danger');
    await expect(alert).toBeVisible();
    await expect(
      alert,
      'the page reads ProblemDetails.detail, so it should name the field at fault',
    ).toContainText(/jersey/i);
  });
});

/**
 * Fills the add-player modal and submits it, returning the API's response so a test can assert
 * on the status the page actually received.
 */
async function addPlayer(
  page: Page,
  player: { firstName: string; lastName: string; jersey: number },
) {
  await page.locator('button:has-text("Add Player")').first().click();

  const modal = page.locator('.modal.show');
  await expect(modal).toBeVisible();

  const teamSelect = modal.locator('select[name="teamId"]');
  if ((await teamSelect.locator('option').count()) < 2) {
    test.skip(true, 'no teams in this environment to add a player to');
  }
  // Index 0 is the "Select Team" placeholder, so the first real team is index 1. Both players
  // in the duplicate-jersey test pick it, which is what makes the numbers clash.
  await teamSelect.selectOption({ index: 1 });

  await modal.locator('input[name="firstName"]').fill(player.firstName);
  await modal.locator('input[name="lastName"]').fill(player.lastName);
  await modal.locator('input[name="jerseyNumber"]').fill(String(player.jersey));
  // The date of birth is what used to break this page: the browser sends a bare date with no
  // zone, and Npgsql rejected it on a timestamptz column, so the save came back a 500.
  await modal.locator('input[name="dateOfBirth"]').fill('1998-07-21');

  const save = page.waitForResponse(
    (response) => response.url().includes('/api/players') && response.request().method() === 'POST',
  );
  await modal.locator('.modal-footer button.btn-primary').click();

  return save;
}

test.describe('what each role is shown', () => {
  test('a team admin is not offered the division admin pages', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.teamAdmin);

    await page.goto('/admin/players');
    await expect(page).toHaveURL(/\/admin\/players/, {
      timeout: 20_000,
    });

    // Clearing league data is a super admin power, so the route must refuse a team admin.
    await page.goto('/admin/clear-data');
    await expect(page).not.toHaveURL(/\/admin\/clear-data/);
  });

  test('a division admin reaches the teams page', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.divisionAdmin);

    await page.goto('/admin/teams');
    await expect(page).toHaveURL(/\/admin\/teams/);
  });
});
