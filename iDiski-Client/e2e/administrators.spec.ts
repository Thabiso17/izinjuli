import { test, expect } from '@playwright/test';
import { accounts, signIn, signInAndWaitForAdmin } from './helpers';

/**
 * Managing administrators — the screen that did not exist, so in production there was no way
 * to onboard anyone at all.
 *
 * The rules behind it are covered against the database in the API tests: who may create whom,
 * and that a division admin cannot mint a peer. These check the screen in front of those rules
 * exists, reaches them, and reports back.
 */
test.describe('administrators', () => {
  test('a super admin can create a team admin scoped to a team', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/users');
    await expect(page).toHaveURL(/\/admin\/users/);

    const before = await page.locator('[data-testid="admin-row"]').count();

    await page.locator('[data-testid="add-admin"]').click();
    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    const email = `team-admin-${Date.now()}@test.com`;
    await modal.locator('input[name="firstName"]').fill('Created');
    await modal.locator('input[name="lastName"]').fill('ByTest');
    await modal.locator('input[name="email"]').fill(email);
    await modal.locator('input[name="password"]').fill('Password123!');

    // Team Admin is the first assignable role.
    await modal.locator('select[name="role"]').selectOption({ index: 1 });

    const teams = modal.locator('.form-check-input');
    if ((await teams.count()) === 0) test.skip(true, 'no teams to assign');

    // A role with no scope leaves someone able to sign in and touch nothing, so the form
    // refuses to submit until a team is chosen.
    const save = modal.locator('[data-testid="save-admin"]');
    await expect(save, 'a team admin with no team should not be creatable').toBeDisabled();

    await teams.first().check();
    await expect(save).toBeEnabled();
    await save.click();

    await expect(page.locator('.alert-success')).toBeVisible();
    await expect(page.locator('[data-testid="admin-row"]')).toHaveCount(before + 1);
    await expect(page.locator(`text=${email}`)).toBeVisible();
  });

  test('the new administrator can sign in', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/users');

    await page.locator('[data-testid="add-admin"]').click();
    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    const email = `signs-in-${Date.now()}@test.com`;
    const password = 'Password123!';

    await modal.locator('input[name="firstName"]').fill('Signs');
    await modal.locator('input[name="lastName"]').fill('In');
    await modal.locator('input[name="email"]').fill(email);
    await modal.locator('input[name="password"]').fill(password);
    // Super Admin needs no scope of its own.
    await modal.locator('select[name="role"]').selectOption({ index: 3 });
    await modal.locator('[data-testid="save-admin"]').click();

    await expect(page.locator('.alert-success')).toBeVisible();

    // The point of creating one: an account that is actually usable. Anything short of this
    // is a row in a table.
    await page.goto('/login');
    await signIn(page, { email, password });
    await expect(page).toHaveURL(/\/admin/, { timeout: 20_000 });
  });

  test('the list says what each administrator is', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
    await page.goto('/admin/users');

    const rows = page.locator('[data-testid="admin-row"]');
    await expect(rows.first()).toBeVisible();

    // A list of administrators that does not say what anyone administers cannot be acted on.
    const roles = await page.locator('[data-testid="admin-roles"]').allInnerTexts();
    expect(roles.length).toBeGreaterThan(0);
    expect(
      roles.some((r) => /Super Admin|Division Admin|Team Admin/.test(r)),
      'no row showed a role, so the list is not reporting access',
    ).toBe(true);
  });

  test('a team admin cannot reach the administrators page', async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.teamAdmin);

    await page.goto('/admin/users');

    // Creating administrators is a super admin power; the route guard has to hold even when
    // somebody types the URL.
    await expect(page).not.toHaveURL(/\/admin\/users/);
  });
});
