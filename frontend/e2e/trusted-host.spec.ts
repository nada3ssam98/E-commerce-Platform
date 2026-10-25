import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { expect, test, type Browser } from '@playwright/test';

// Through the local edge (dev/caddy/Caddyfile): ADR-04 rules 1-8 end to end, using the API and SSR test-only
// diagnostics (§4d). Requests are made by the browser, so forged headers travel exactly as a client would send them.

const port = process.env['E2E_EDGE_PORT'] ?? '8443';
const brandA = `https://brand-a.localhost:${port}`;
const admin = `https://admin.localhost:${port}`;

const forgedHosts = {
  'X-Forwarded-Host': 'brand-b.localhost',
  Forwarded: 'host=brand-b.localhost',
  'X-Platform-Host': 'brand-b.localhost',
};
const forgedCredentials = {
  'X-Platform-Api-Edge-Key': 'client-forged-api-edge-key-0123456789abcdef',
  'X-Platform-Ssr-Api-Key': 'client-forged-ssr-api-key-0123456789abcdef',
  'X-Platform-Ssr-Ingress-Key': 'client-forged-ingress-key-0123456789abcdef',
};

interface ApiRequestContext {
  caller: string;
  platformHost: string;
  path: string;
  credentialHeaders: { apiEdgeKey: boolean; ssrApiKey: boolean; ssrIngressKey: boolean };
}

interface SsrRequestContext {
  platformHost: string;
  credentialHeaders: Record<string, boolean>;
}

/** Opens a URL in a fresh browser context that sends the given extra headers; returns status and body. */
async function open(browser: Browser, url: string, headers: Record<string, string> = {}) {
  const context = await browser.newContext({ extraHTTPHeaders: headers });
  try {
    const page = await context.newPage();
    const response = await page.goto(url);
    expect(response, `no response for ${url}`).not.toBeNull();
    return { status: response!.status(), url: page.url(), body: await response!.text() };
  } finally {
    await context.close();
  }
}

async function openJson<T>(browser: Browser, url: string, headers: Record<string, string> = {}): Promise<T> {
  const { status, body } = await open(browser, url, headers);
  expect(status, `${url}: ${body}`).toBe(200);
  return JSON.parse(body) as T;
}

test('a store page is rendered on the server with a value from the SSR internal API call (M0-2, TH-4)', async ({ browser }) => {
  const page = await open(browser, `${brandA}/`);

  expect(page.status).toBe(200);
  expect(page.url).toMatch(new RegExp(`^${brandA}/(en|ar)/$`));
  // Initial HTML from the server, before any browser-side code runs.
  expect(page.body).toMatch(/data-testid="api-health"[^>]*>Healthy</);

  const call = await openJson<ApiRequestContext>(browser, `${brandA}/api/_test/last-ssr-call`);
  expect(call).toMatchObject({
    caller: 'Ssr',
    platformHost: 'brand-a.localhost',
    path: '/api/health',
    credentialHeaders: { apiEdgeKey: false, ssrApiKey: true, ssrIngressKey: false },
  });
});

test('the admin placeholder is served and /api/health is same-origin on both hosts (M0-3)', async ({ browser }) => {
  const adminPage = await open(browser, `${admin}/`);
  expect(adminPage.status).toBe(200);
  expect(adminPage.body).toContain('<app-root');

  for (const origin of [brandA, admin]) {
    expect(await openJson(browser, `${origin}/api/health`)).toEqual({ status: 'Healthy' });
  }
});

test('forged host headers from the browser never change the store host (M0-4, TH-1, TH-4)', async ({ browser }) => {
  const apiContext = await openJson<ApiRequestContext>(browser, `${brandA}/api/_test/request-context`, forgedHosts);
  expect(apiContext).toMatchObject({ caller: 'Edge', platformHost: 'brand-a.localhost' });

  const ssrContext = await openJson<SsrRequestContext>(browser, `${brandA}/_test/ssr-request-context`, forgedHosts);
  expect(ssrContext.platformHost).toBe('brand-a.localhost');

  const page = await open(browser, `${brandA}/en/`, forgedHosts);
  expect(page.body).toMatch(/data-testid="api-health"[^>]*>Healthy</);
  const call = await openJson<ApiRequestContext>(browser, `${brandA}/api/_test/last-ssr-call`);
  expect(call.platformHost).toBe('brand-a.localhost');
});

test('each route receives exactly one edge-set credential, never a client copy (M0-8c, TH-13)', async ({ browser }) => {
  const ssrContext = await openJson<SsrRequestContext>(browser, `${brandA}/_test/ssr-request-context`, forgedCredentials);
  expect(ssrContext.credentialHeaders).toEqual({
    'x-platform-ssr-ingress-key': true,
    'x-platform-api-edge-key': false,
    'x-platform-ssr-api-key': false,
  });

  for (const origin of [brandA, admin]) {
    const apiContext = await openJson<ApiRequestContext>(browser, `${origin}/api/_test/request-context`, forgedCredentials);
    expect(apiContext.caller).toBe('Edge');
    expect(apiContext.credentialHeaders).toEqual({ apiEdgeKey: true, ssrApiKey: false, ssrIngressKey: false });
  }
});

test('no key value or internal API URL reaches the browser or the logs (M0-7, M0-10)', async ({ browser }) => {
  const keys = JSON.parse(process.env['E2E_KEY_VALUES'] ?? '[]') as string[];
  const internalApiUrl = process.env['E2E_INTERNAL_API_URL'] ?? '';
  expect(keys).toHaveLength(3);
  expect(internalApiUrl).not.toBe('');

  const context = await browser.newContext();
  const bodies: string[] = [];
  const page = await context.newPage();
  page.on('response', async (response) => bodies.push(await response.text().catch(() => '')));
  await page.goto(`${brandA}/ar/`);
  await page.goto(`${admin}/`);
  await page.waitForLoadState('networkidle');
  await context.close();

  const logDirectory = process.env['E2E_LOG_DIRECTORY'] ?? '';
  const logs = readdirSync(logDirectory).map((file) => readFileSync(join(logDirectory, file), 'utf8'));
  expect(bodies.length).toBeGreaterThan(0);
  for (const body of bodies) {
    for (const value of [...keys, internalApiUrl]) {
      expect(body.includes(value)).toBe(false);
    }
  }
  for (const log of logs) {
    for (const value of keys) {
      expect(log.includes(value)).toBe(false);
    }
  }
});
