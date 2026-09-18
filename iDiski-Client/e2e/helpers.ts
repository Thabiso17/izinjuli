import { Page, expect } from '@playwright/test';

/**
 * The accounts Program.cs seeds when it starts in Development. Using them keeps the browser
 * tests going through the real login form rather than forging a token.
 */
export const accounts = {
  superAdmin: { email: 'superadmin@test.com', password: 'Password123!' },
  // The address predates the role's name: divadmin@test.com is a competition admin now,
  // assigned to the competitions they run rather than to a division.
  competitionAdmin: { email: 'divadmin@test.com', password: 'Password123!' },
  teamAdmin: { email: 'teamadmin@test.com', password: 'Password123!' },
  inactive: { email: 'inactive@test.com', password: 'Password123!' },
};

export async function signIn(page: Page, account: { email: string; password: string }) {
  await page.goto('/login');
  await page.locator('#email').fill(account.email);
  await page.locator('#password').fill(account.password);
  await page.locator('button.login-button').click();
}

export async function signInAndWaitForAdmin(
  page: Page,
  account: { email: string; password: string },
) {
  await signIn(page, account);
  await expect(page).toHaveURL(/\/admin/, { timeout: 20_000 });
}

/**
 * Jersey numbers are unique per team, so two players created in the same run must never pick
 * the same one. A counter guarantees that within a run; the starting point moves with the
 * clock so a rerun against a database that still holds the last run's players is unlikely to
 * land on them again.
 */
let jersey = 20 + (Math.floor(Date.now() / 1000) % 60);

export function someJerseyNumber() {
  jersey = jersey >= 99 ? 20 : jersey + 1;
  return jersey;
}
