import { test, expect, type Page } from '@playwright/test';
import { accounts, signInAndWaitForAdmin } from './helpers';

/**
 * The admin pages nothing was driving: articles, videos, sponsors and the layout editor.
 *
 * They were the last four without a browser test, and the filter bugs are the argument for
 * covering them: five of those reached the user with CI green, because the suite drove
 * journeys and never read a page back. A save that returns 500, a form that posts the wrong
 * shape, a list that renders nothing — none of it fails loudly anywhere else.
 *
 * These stay at the level that catches those: the page loads, the form saves, and what was
 * saved comes back on the page.
 */
test.describe('content admin', () => {
  test.beforeEach(async ({ page }) => {
    await signInAndWaitForAdmin(page, accounts.superAdmin);
  });

  test('an article can be written and appears in the list', async ({ page }) => {
    await page.goto('/admin/articles');
    await page.waitForLoadState('networkidle');

    const title = `Match report ${unique()}`;

    await page.locator('[data-testid="add-article"]').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('input[name="title"]').fill(title);
    await modal.locator('textarea[name="content"]').fill(
      'A full account of a match that did not happen, written to prove the page saves.',
    );
    await modal.locator('input[name="author"]').fill('The Test Desk');

    await saveAndExpectSuccess(page, modal, 'save-article');

    // The slug is generated from the title, which is the part most likely to go wrong
    // silently — a clash or an empty slug would surface here as a save that never lands.
    await expect(page.locator(`text=${title}`).first()).toBeVisible();
  });

  test('a video can be added with its URL', async ({ page }) => {
    await page.goto('/admin/videos');
    await page.waitForLoadState('networkidle');

    const title = `Highlights ${unique()}`;

    await page.locator('[data-testid="add-video"]').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('input[name="title"]').fill(title);
    await modal.locator('input[name="videoUrl"]').fill(
      'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    );
    await modal.locator('input[name="author"]').fill('The Test Desk');

    await saveAndExpectSuccess(page, modal, 'save-video');

    await expect(page.locator(`text=${title}`).first()).toBeVisible();
  });

  test('a sponsor can be added with a tier and a placement', async ({ page }) => {
    await page.goto('/admin/sponsors');
    await page.waitForLoadState('networkidle');

    const name = `Sponsor ${unique()}`;

    await page.locator('[data-testid="add-sponsor"]').first().click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible();

    await modal.locator('input[name="name"]').fill(name);
    await modal.locator('select[name="tier"]').selectOption('Gold');
    await modal.locator('select[name="placement"]').selectOption('Homepage');
    await modal.locator('input[name="displayOrder"]').fill('1');

    await saveAndExpectSuccess(page, modal, 'save-sponsor');

    await expect(page.locator(`text=${name}`).first()).toBeVisible();
  });

  test('a sponsor with no tier is not saved', async ({ page }) => {
    await page.goto('/admin/sponsors');
    await page.waitForLoadState('networkidle');

    await page.locator('[data-testid="add-sponsor"]').first().click();

    const modal = page.locator('.modal.show');
    await modal.locator('input[name="name"]').fill(`Untiered ${unique()}`);

    // Tier and placement are what decide where a sponsor is shown at all, so a sponsor
    // without them would be paid for and never appear.
    await expect(modal.locator('[data-testid="save-sponsor"]')).toBeDisabled();
  });

  test('the layout editor saves a change to what visitors see', async ({ page }) => {
    await page.goto('/admin/layout');
    await page.waitForLoadState('networkidle');

    // Worth pinning at all because this page decides what the public homepage shows: if it
    // stops loading or stops saving, there is no way to change that.
    await expect(page).toHaveURL(/\/admin\/layout/);

    // The editor loaded rather than failing: the error state is the other thing this page
    // can render, and it looks much like an empty one at a glance.
    await expect(page.locator('text=Failed to load layout')).toHaveCount(0);

    const save = page.locator('[data-testid="save-layout"]');
    await expect(save).toBeVisible();

    const toggles = page.locator('[data-testid="toggle-visibility"]');

    // The board lists every component the site can render, configured or not. It used to list
    // only saved rows, so a database with no layout rows gave an empty board — and since the
    // public homepage reads the same rows, it rendered nothing, with no way to fix it from
    // the one screen that exists to fix it.
    await expect(
      toggles.first(),
      'the editor must offer something to arrange even before anything is configured',
    ).toBeVisible();

    const count = await toggles.count();
    expect(count, 'every registered component should be listed').toBeGreaterThanOrEqual(5);

    await toggles.first().click();
    await expect(save).toBeEnabled();

    const saved = page.waitForResponse(
      (r) => r.url().includes('/pagelayoutconfigs') && r.request().method() !== 'GET',
      { timeout: 20_000 },
    );

    await save.click();

    const response = await saved;
    expect(response.status(), await response.text()).toBeLessThan(400);

    await expect(page.locator('text=Layout saved successfully')).toBeVisible();

    // Put it back, so a rerun starts from the same homepage this one did.
    await toggles.first().click();
    await save.click();
  });

  test('the homepage shows its sections even with nothing configured', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // The homepage is built from the same layout rows the editor writes, and it had no
    // fallback: an empty table meant an empty page for every visitor. A fresh install
    // showed nothing at all.
    await expect(page.locator('app-root')).not.toHaveText('');
    await expect(page.locator('text=Failed to load page layout')).toHaveCount(0);

    // Whatever the configuration, the page has content rather than a bare shell.
    const sections = page.locator('section, article, .card');
    expect(await sections.count(), 'the homepage rendered no sections at all')
      .toBeGreaterThan(0);
  });
});

// ── helpers ──────────────────────────────────────────────────────────────────

function unique() {
  return `${Date.now()}${Math.floor(Math.random() * 1000)}`.slice(-8);
}

/**
 * Saves and insists the page says so. A modal that closes without a success message is the
 * shape a silent failure takes here — the row is gone from the screen either way.
 */
async function saveAndExpectSuccess(page: Page, modal: ReturnType<Page['locator']>, testId: string) {
  // Disabled here means a required field is missing, and clicking would do nothing at all
  // while the test waited for a success that was never coming.
  const save = modal.locator(`[data-testid="${testId}"]`);
  await expect(save).toBeEnabled();
  await save.click();

  // Success first: it is the assertion that waits for the request to land. Checking for an
  // error before that would pass while the response was still in flight.
  await expect(page.locator('.alert-success')).toBeVisible();
  await expect(page.locator('.alert-danger')).toHaveCount(0);
  await expect(modal).toBeHidden();
}
