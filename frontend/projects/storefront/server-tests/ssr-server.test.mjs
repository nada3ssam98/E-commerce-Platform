// Integration tests of the BUILT SSR server (run after `ng build storefront`): `npm run test:ssr`.
// The API is replaced by a FAKE (test fixture only): a local HTTP server that records the requests the SSR
// server makes and answers like GET /api/health does. It is not a model of production behavior beyond that.
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { fileURLToPath } from 'node:url';
import { after, before, describe, it } from 'node:test';

const serverEntry = fileURLToPath(new URL('../../../dist/storefront/server/server.mjs', import.meta.url));
const ingressKey = 'test-ingress-key-0123456789abcdef0123456789';
const previousIngressKey = 'test-previous-ingress-key-0123456789abcdef';
const ssrApiKey = 'test-ssr-api-key-0123456789abcdef0123456789';

/** Starts the built server; resolves with its port once listening, or with its exit code and stderr if it stops. */
function startServer(settings) {
  const child = spawn(process.execPath, [serverEntry], {
    env: { PATH: process.env.PATH ?? '', SystemRoot: process.env.SystemRoot ?? '', PORT: '0', ...settings },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let stdout = '';
  let stderr = '';
  child.stdout.on('data', (chunk) => (stdout += chunk));
  child.stderr.on('data', (chunk) => (stderr += chunk));
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      child.kill();
      reject(new Error(`SSR server did not start or stop in time.\n${stdout}\n${stderr}`));
    }, 30_000);
    child.stdout.on('data', () => {
      const match = /listening on http:\/\/localhost:(\d+)/.exec(stdout);
      if (match) {
        clearTimeout(timer);
        resolve({ child, port: Number(match[1]), output: () => stdout + stderr });
      }
    });
    child.on('exit', (code) => {
      clearTimeout(timer);
      resolve({ exitCode: code, stderr, stdout });
    });
  });
}

function startFakeApi() {
  const calls = [];
  const server = createServer((request, response) => {
    calls.push({ method: request.method, url: request.url, headers: request.headers });
    response.setHeader('content-type', 'application/json');
    response.end(JSON.stringify({ status: 'Healthy' }));
  });
  return new Promise((resolve) => server.listen(0, '127.0.0.1', () => resolve({ server, calls, origin: `http://127.0.0.1:${server.address().port}` })));
}

const baseSettings = (apiOrigin) => ({
  PLATFORM_SSR_INGRESS_KEY: ingressKey,
  PLATFORM_SSR_INGRESS_KEY_PREVIOUS: previousIngressKey,
  PLATFORM_SSR_API_KEY: ssrApiKey,
  INTERNAL_API_BASE_URL: apiOrigin,
});

describe('SSR server configuration on start (M0-9, TH-9, TH-14)', () => {
  it('refuses to start without configuration and names every missing setting', async () => {
    const result = await startServer({});

    assert.equal(result.exitCode, 1);
    for (const name of ['PLATFORM_SSR_INGRESS_KEY', 'PLATFORM_SSR_API_KEY', 'INTERNAL_API_BASE_URL']) {
      assert.match(result.stderr, new RegExp(`Missing setting '${name}'`));
    }
  });

  it('refuses a weak key without printing it', async () => {
    const result = await startServer({ ...baseSettings('http://127.0.0.1:1'), PLATFORM_SSR_INGRESS_KEY: 'weak-secret-value' });

    assert.equal(result.exitCode, 1);
    assert.match(result.stderr, /'PLATFORM_SSR_INGRESS_KEY' must be at least 32 characters/);
    assert.doesNotMatch(result.stderr + result.stdout, /weak-secret-value/);
  });

  it('refuses to start when an API edge key is supplied (TH-14)', async () => {
    const result = await startServer({ ...baseSettings('http://127.0.0.1:1'), PLATFORM_API_EDGE_KEY: 'edge-key-must-not-be-here-0123456789' });

    assert.equal(result.exitCode, 1);
    assert.match(result.stderr, /'PLATFORM_API_EDGE_KEY' must not be provided to the SSR server/);
    assert.doesNotMatch(result.stderr, /edge-key-must-not-be-here/);
  });

  it('refuses the test diagnostic outside Testing (TH-9)', async () => {
    const result = await startServer({ ...baseSettings('http://127.0.0.1:1'), PLATFORM_ENABLE_TEST_DIAGNOSTICS: 'true' });

    assert.equal(result.exitCode, 1);
    assert.match(result.stderr, /'PLATFORM_ENABLE_TEST_DIAGNOSTICS' is allowed only when 'PLATFORM_ENVIRONMENT' is 'Testing'/);
  });
});

describe('SSR server in Production', () => {
  let api;
  let ssr;
  const url = (path) => `http://localhost:${ssr.port}${path}`;
  const trusted = { 'x-platform-ssr-ingress-key': ingressKey, 'x-platform-host': 'brand-a.localhost' };

  before(async () => {
    api = await startFakeApi();
    ssr = await startServer(baseSettings(api.origin));
    assert.ok(ssr.port, 'server must start');
  });

  after(() => {
    ssr.child?.kill();
    api.server.close();
  });

  for (const [name, headers] of [
    ['no key', { 'x-platform-host': 'brand-a.localhost' }],
    ['a wrong key', { 'x-platform-ssr-ingress-key': 'wrong-key', 'x-platform-host': 'brand-a.localhost' }],
    ['a forged host header and no key', { 'x-platform-host': 'brand-b.localhost', 'x-forwarded-host': 'brand-b.localhost' }],
    ['the API edge key instead of the ingress key', { 'x-platform-api-edge-key': ingressKey, 'x-platform-host': 'brand-a.localhost' }],
    ['a valid key but no platform host', { 'x-platform-ssr-ingress-key': ingressKey }],
  ]) {
    it(`rejects a request with ${name}: 403, nothing rendered, no API call (TH-3)`, async () => {
      const callsBefore = api.calls.length;

      const response = await fetch(url('/ar/'), { headers });

      assert.equal(response.status, 403);
      assert.equal(response.headers.get('cache-control'), 'no-store');
      const body = await response.text();
      assert.equal(body, 'Forbidden');
      assert.equal(api.calls.length, callsBefore);
    });
  }

  it('renders with the current or the previous ingress key (rotation)', async () => {
    for (const key of [ingressKey, previousIngressKey]) {
      const response = await fetch(url('/en/'), { headers: { ...trusted, 'x-platform-ssr-ingress-key': key } });
      assert.equal(response.status, 200);
    }
  });

  it('renders the page server-side with the API status from the internal call (M0-2, SSR side)', async () => {
    const response = await fetch(url('/ar/'), { headers: trusted });

    assert.equal(response.status, 200);
    const html = await response.text();
    assert.match(html, /<html lang="ar" dir="rtl"/);
    assert.match(html, /data-testid="api-health"[^>]*>Healthy</);
    assert.doesNotMatch(html, new RegExp(`${ingressKey}|${ssrApiKey}|${api.origin.replace(/[.]/g, '\\.')}`));
  });

  it('calls the API with GET, the verified host, and only the SSR-to-API key (TH-4, rule 7)', async () => {
    const callsBefore = api.calls.length;

    await fetch(url('/en/'), {
      headers: {
        ...trusted,
        'x-forwarded-host': 'brand-b.localhost',
        cookie: '__Host-sf_session=browser-session-cookie',
        authorization: 'Bearer browser-token',
        'x-platform-api-edge-key': 'client-forged-edge-key',
      },
    });

    const calls = api.calls.slice(callsBefore);
    assert.equal(calls.length, 1);
    const [call] = calls;
    assert.equal(call.method, 'GET');
    assert.equal(call.url, '/api/health');
    assert.equal(call.headers['x-platform-host'], 'brand-a.localhost');
    assert.equal(call.headers['x-platform-ssr-api-key'], ssrApiKey);
    for (const forbidden of ['cookie', 'authorization', 'x-platform-ssr-ingress-key', 'x-platform-api-edge-key', 'x-forwarded-host']) {
      assert.equal(call.headers[forbidden], undefined, `${forbidden} must not be forwarded`);
    }
  });

  it('has no test diagnostic in Production (TH-9)', async () => {
    const response = await fetch(url('/_test/ssr-request-context'), { headers: trusted });

    assert.equal(response.status, 404);
  });

  it('never logs key values', () => {
    const output = ssr.output();
    assert.doesNotMatch(output, new RegExp(`${ingressKey}|${previousIngressKey}|${ssrApiKey}`));
    assert.match(output, /"reason":"missing-ingress-key"/);
  });
});

describe('SSR server in Testing with the diagnostic enabled', () => {
  let ssr;

  before(async () => {
    ssr = await startServer({ ...baseSettings('http://127.0.0.1:1'), PLATFORM_ENVIRONMENT: 'Testing', PLATFORM_ENABLE_TEST_DIAGNOSTICS: 'true' });
    assert.ok(ssr.port, 'server must start');
  });

  after(() => ssr.child?.kill());

  it('reports which credential headers arrived, never their values (supports TH-13)', async () => {
    const response = await fetch(`http://localhost:${ssr.port}/_test/ssr-request-context`, {
      headers: { 'x-platform-ssr-ingress-key': ingressKey, 'x-platform-host': 'Brand-A.localhost' },
    });

    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), {
      platformHost: 'brand-a.localhost',
      credentialHeaders: { 'x-platform-ssr-ingress-key': true, 'x-platform-api-edge-key': false, 'x-platform-ssr-api-key': false },
    });
  });

  it('still requires the ingress key for the diagnostic', async () => {
    const response = await fetch(`http://localhost:${ssr.port}/_test/ssr-request-context`, { headers: { 'x-platform-host': 'brand-a.localhost' } });

    assert.equal(response.status, 403);
  });
});
