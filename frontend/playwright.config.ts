import { defineConfig, devices } from '@playwright/test';

/**
 * E2E tests through the local edge (ADR-11; milestone-m0-m1 M0-2, M0-3, M0-4, M0-6, M0-8c). `npm run e2e` starts the
 * stack itself (e2e/stack.mjs). The edge uses Caddy's local CA, which the test browser does not trust.
 */
export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.mjs',
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env['CI']),
  retries: 0,
  reporter: process.env['CI'] ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    ...devices['Desktop Chrome'],
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    launchOptions: {
      // *.localhost always means this machine, whatever the operating system's resolver does.
      args: ['--host-resolver-rules=MAP *.localhost 127.0.0.1'],
    },
  },
  projects: [{ name: 'chromium' }],
});
