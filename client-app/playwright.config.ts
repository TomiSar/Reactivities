import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
    testDir: './tests',
    fullyParallel: true,
    reporter: 'html',
    use: {
        // Frontend URL
        baseURL: 'http://localhost:3000',
        trace: 'on-first-retry',
    },

    // Start both servers
    webServer: [
        {
            // 1. .NET BACKEND
            command: 'dotnet run',
            cwd: '../API', // Menee API-kansioon ajamaan komennon
            url: 'http://localhost:5000',
            timeout: 120 * 1000,
        },
        {
            // 2. REACT FRONTEND
            command: 'npm run start',
            cwd: './',
            url: 'http://localhost:3000',
        },
    ],

    projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
