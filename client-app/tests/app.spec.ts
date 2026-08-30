/* eslint-disable testing-library/prefer-screen-queries */
import { expect, test } from '@playwright/test';

test.describe('Authentication flows', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/');
    });

    test('Application opens at Register and Login page', async ({ page }) => {
        await expect(page).toHaveTitle(/Reactivities/);
        await expect(page.getByRole('button', { name: /Login/i })).toBeVisible();
        await expect(page.getByRole('button', { name: /Register/i })).toBeVisible();
    });

    test('User can login and is redirected directly to activities', async ({ page }) => {
        await page.getByRole('button', { name: /Login/i }).click();

        await page.getByPlaceholder('Email').fill('bob@test.com');
        await page.getByPlaceholder('Password').fill('Pa$$w0rd');
        await page.locator('form').getByRole('button', { name: 'Login' }).click();

        // userStore.ts: login -> router.navigate('/activities')
        await expect(page).toHaveURL(/.*activities/);
        // Check that Navbar is visible (loading success)
        await expect(page.getByRole('link', { name: 'Activities', exact: true })).toBeVisible();
    });

    test('User login with invalid credentials returns error label', async ({ page }) => {
        await page.getByRole('button', { name: /Login/i }).click();

        await page.getByPlaceholder('Email').fill('fake@mail.com');
        await page.getByPlaceholder('Password').fill('vääräsalasana');
        await page.locator('form').getByRole('button', { name: 'Login' }).click();

        // LoginForm.tsx renders label component is error occurs
        const errorLabel = page.locator('.ui.label.red');
        await expect(errorLabel).toBeVisible();
        await expect(errorLabel).toHaveText('Invalid email or password');
    });

    test('User can register, sees welcome message, and can navigate to activities', async ({ page }) => {
        const randomUser = createRandomUser();
        const userLower = randomUser.toLowerCase();

        await page.getByRole('button', { name: /Register/i }).click();

        await page.getByPlaceholder('Display Name').fill(randomUser);
        await page.getByPlaceholder('Username').fill(userLower);
        await page.getByPlaceholder('Email').fill(`${userLower}@test.com`);
        await page.getByPlaceholder('Password').fill('Pa$$w0rd');

        await page.locator('form').getByRole('button', { name: 'Register' }).click();

        // userStore.ts: register -> router.navigate('/')
        // HomePage.tsx: shows "Welcome {username}"
        await expect(page).toHaveURL('http://localhost:3000/');
        await expect(page.getByRole('heading', { name: `Welcome ${userLower}`, level: 2 })).toBeVisible();

        const activitiesButton = page.getByRole('button', { name: /Go to activities/i });
        await expect(activitiesButton).toBeVisible();

        await activitiesButton.click();
        await expect(page).toHaveURL(/.*activities/);
    });
});

const createRandomUser = () => {
    const chars = 'abcdefghijklmnopqrstuvwxyz';
    let randomUser = '';
    for (let i = 0; i < 8; i++) {
        randomUser += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return randomUser;
};
