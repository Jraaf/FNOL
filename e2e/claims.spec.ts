import { expect, Page, test } from '@playwright/test';

// End-to-end test that drives the SPA at http://127.0.0.1:4200 against the API
// at http://localhost:5000. Servers are expected to already be running.

const API = 'http://localhost:5000';

async function switchRole(page: Page, role: 'Handler' | 'Supervisor' | 'Manager') {
  // The toolbar button shows "<Name> (<Role>)" — match any current state.
  await page.locator('mat-toolbar button:has(mat-icon:has-text("person"))').click();
  await page.getByRole('menuitem', { name: `Switch to ${role}` }).click();
  // Material menus animate closed; ensure the toolbar reflects the new role before continuing.
  await expect(page.locator('mat-toolbar')).toContainText(role);
}

async function typeAutocomplete(page: Page, label: string, query: string) {
  // Use the combobox role so we don't also match the autocomplete listbox panel,
  // which also gets the form-field label as its aria-labelledby.
  const input = page.getByRole('combobox', { name: label });
  await input.click();
  await input.pressSequentially(query, { delay: 30 });
}

test.describe.serial('Claims module — full FNOL → reserve → approve → upload → status', () => {
  let claimNumber: string;

  test('1. Dashboard loads with seeded reference data', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/DICEUS Claims/);
    await expect(page.getByRole('heading', { name: 'Claims Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Log New Claim/i })).toBeVisible();
  });

  test('2. FNOL multi-step form creates a claim and the detail screen shows a success banner', async ({ page }) => {
    await page.goto('/claims/new');
    await expect(page.getByRole('heading', { name: /Log New Claim/i })).toBeVisible();

    // -- Step 1 --
    // Type into the policy autocomplete — pressSequentially fires the input events
    // Material's matAutocomplete needs to open its panel.
    await typeAutocomplete(page, 'Search policy', 'POL-2026-0000001');
    await page.getByRole('option', { name: /POL-2026-0000001/ }).first().click();

    await page.getByLabel('Loss date').fill('2026-04-15');

    // mat-select for Cause of loss
    await page.getByLabel('Cause of loss').click();
    await page.getByRole('option', { name: /^COLLISION/ }).click();

    await page.getByLabel('Loss location').fill('I-405 / SR-520 interchange, Bellevue WA');
    await page.getByLabel('Loss description').fill('Rear-end at low speed; minor bumper damage. Driven via E2E test.');
    await page.getByRole('button', { name: 'Next' }).first().click();

    // -- Step 2 --
    await expect(page.getByRole('heading', { name: 'Parties' })).toBeVisible();
    // Fill the default Claimant row (FormArray index 0)
    const firstName = page.getByLabel('First name').first();
    const lastName = page.getByLabel('Last name').first();
    const email = page.getByLabel('Email').first();
    await firstName.fill('Casey');
    await lastName.fill('Claimant');
    await email.fill('casey@example.com');
    await page.getByRole('button', { name: 'Next' }).first().click();

    // -- Step 3 --
    // Material checkbox renders as <mat-checkbox>label<input type=checkbox></mat-checkbox>;
    // The component template uses a plain HTML checkbox wrapped in a <label>, so getByLabel works.
    await page.getByLabel(/Open an initial reserve/i).check();
    await page.getByLabel('Amount (USD)').fill('50000');
    await expect(page.getByText(/Supervisor approval required/i)).toBeVisible();
    await page.getByRole('button', { name: 'Submit Claim' }).click();

    // Wait for navigation to the detail page
    await page.waitForURL(/\/claims\/[0-9a-f-]{36}$/, { timeout: 15000 });

    // Success banner
    const banner = page.locator('.success-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText(/CLM-\d{4}-\d{7}/);
    claimNumber = (await banner.textContent())!.match(/CLM-\d{4}-\d{7}/)![0];

    // Header also shows the claim number
    await expect(page.getByRole('heading', { name: claimNumber })).toBeVisible();
  });

  test('3. Supervisor approves the pending reserve via the role-gated action', async ({ page }) => {
    await page.goto('/claims');
    await page.getByRole('cell', { name: claimNumber }).click();
    await page.waitForURL(/\/claims\/[0-9a-f-]{36}$/);

    // Reserves tab
    await page.getByRole('tab', { name: /Reserves/ }).click();

    // As Handler the Approve button must not be visible
    await expect(page.getByRole('button', { name: 'Approve', exact: true })).toHaveCount(0);

    // Switch to Supervisor and approve
    await switchRole(page, 'Supervisor');
    const approveBtn = page.getByRole('button', { name: 'Approve', exact: true });
    await expect(approveBtn).toBeVisible();
    await approveBtn.click();

    // Snackbar
    await expect(page.locator('mat-snack-bar-container, .mat-mdc-snack-bar-container'))
      .toContainText(/Reserve approved/i, { timeout: 5000 });

    // Approval status shows as "Approved" in the reserves table
    await expect(page.locator('table.reserves')).toContainText('Approved');
  });

  test('4. Status transition opens a Material confirmation dialog', async ({ page }) => {
    await page.goto('/claims');
    await page.getByRole('cell', { name: claimNumber }).click();
    await page.waitForURL(/\/claims\/[0-9a-f-]{36}$/);

    // Click Change Status -> first transition (Draft -> Open)
    await page.getByRole('button', { name: /Change Status/i }).click();
    await page.getByRole('menuitem', { name: /Open/ }).first().click();

    // Confirmation dialog appears
    const dialog = page.locator('mat-dialog-container, .mat-mdc-dialog-container');
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText(/Transition to Open\?/);

    // Cancel -> dialog closes, status unchanged
    await dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(dialog).toHaveCount(0);

    // Reopen -> confirm
    await page.getByRole('button', { name: /Change Status/i }).click();
    await page.getByRole('menuitem', { name: /Open/ }).first().click();
    await page.locator('mat-dialog-container, .mat-mdc-dialog-container')
      .getByRole('button', { name: /Move to Open/ })
      .click();

    await expect(page.locator('mat-snack-bar-container, .mat-mdc-snack-bar-container'))
      .toContainText(/Status changed to Open/, { timeout: 5000 });
    await expect(page.locator('app-status-badge .status-badge').first())
      .toContainText(/Open/, { timeout: 5000 });
  });

  test('5. Documents tab uploads a file and lists it', async ({ page }) => {
    await page.goto('/claims');
    await page.getByRole('cell', { name: claimNumber }).click();
    await page.waitForURL(/\/claims\/[0-9a-f-]{36}$/);

    await page.getByRole('tab', { name: /Documents/ }).click();

    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles({
      name: 'e2e-evidence.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('Police report - incident E2E-001')
    });

    await expect(page.locator('mat-snack-bar-container, .mat-mdc-snack-bar-container'))
      .toContainText(/Uploaded e2e-evidence.txt/, { timeout: 10000 });

    const docLink = page.getByRole('link', { name: 'e2e-evidence.txt' });
    await expect(docLink).toBeVisible();
  });

  test('6. Audit Log is reverse-chronological with the full lifecycle of events', async ({ page }) => {
    await page.goto('/claims');
    await page.getByRole('cell', { name: claimNumber }).click();
    await page.waitForURL(/\/claims\/[0-9a-f-]{36}$/);

    await page.getByRole('tab', { name: /Audit Log/ }).click();
    await expect(page.locator('code').first()).toBeVisible();

    const events = await page.locator('table code').allTextContents();
    expect(events.length).toBeGreaterThan(4);

    // Reverse-chronological: CLAIM_CREATED is the oldest, so it must be among the last few
    // events. Events written in the same handler share OccurredAt to the tick, so SQL ORDER BY
    // can return them in any relative order — we just require CLAIM_CREATED to be in the tail.
    expect(events).toContain('CLAIM_CREATED');
    const tail = events.slice(-3);
    expect(tail, `tail=${tail.join('|')}`).toContain('CLAIM_CREATED');

    const flat = events.join(',');
    expect(flat).toMatch(/RESERVE_OPENED/);
    expect(flat).toMatch(/RESERVE_APPROVED/);
    expect(flat).toMatch(/GL_POSTING_SIMULATED/);
    expect(flat).toMatch(/CLAIM_STATUS_CHANGED/);
    expect(flat).toMatch(/DOCUMENT_UPLOADED/);
  });

  test('7. Dashboard filter (status=Open) shows the now-Open claim', async ({ page }) => {
    await page.goto('/claims');
    await page.getByLabel('Status').click();
    await page.getByRole('option', { name: 'Open', exact: true }).click();
    await expect(page.getByRole('cell', { name: claimNumber })).toBeVisible();
  });

  test('8. Manager-tier reserve approval gating: Handler/Supervisor blocked, Manager allowed', async ({ page }) => {
    // Open a manager-tier reserve via the API for speed.
    const res = await fetch(`${API}/api/claims`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Mock-Role': 'Handler',
        'X-Mock-UserId': 'handler-1'
      },
      body: JSON.stringify({
        policyId: 'aaaaaaaa-1111-1111-1111-111111111111',
        lossDate: '2026-03-01T00:00:00Z',
        causeOfLossCode: 'FIRE',
        lossLocation: 'Warehouse 5',
        lossDescription: 'Manager-tier test (E2E)',
        parties: [{ partyType: 0, firstName: 'Big', lastName: 'Loss' }],
        riskObjects: [],
        initialReserve: { componentType: 0, amount: 250000 }
      })
    });
    expect(res.status).toBe(201);
    const created = await res.json();

    await page.goto(`/claims/${created.claimId}`);
    await page.getByRole('tab', { name: /Reserves/ }).click();

    // Handler: no Approve button
    await expect(page.getByRole('button', { name: 'Approve', exact: true })).toHaveCount(0);

    // Supervisor: still no Approve button (manager-tier requires Manager)
    await switchRole(page, 'Supervisor');
    await expect(page.getByRole('button', { name: 'Approve', exact: true })).toHaveCount(0);

    // Manager: Approve appears
    await switchRole(page, 'Manager');
    await expect(page.getByRole('button', { name: 'Approve', exact: true })).toBeVisible();
  });
});
