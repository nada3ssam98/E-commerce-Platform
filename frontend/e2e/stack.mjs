// Starts the full local stack for the E2E tests, without Aspire: the built API, the built SSR server, the built admin
// bundle, and the local edge (Caddy, dev/caddy/Caddyfile). Every request in the tests goes through the edge.
//
// Prerequisites: `dotnet build backend/src/Api -c Release`, `npm run build:all`, and a Caddy binary
// (CADDY_BIN, or `caddy` on PATH). No database is needed: M0 pages read only GET /api/health (liveness).
// Keys are random per run and given to each process only as its own (ADR-04 §4b).
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { createWriteStream, existsSync, mkdirSync, mkdtempSync, readFileSync } from 'node:fs';
import { createServer, request as httpRequest } from 'node:http';
import { connect } from 'node:net';
import { tmpdir } from 'node:os';
import { extname, join, normalize, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const frontend = resolve(fileURLToPath(new URL('..', import.meta.url)));
const repository = resolve(frontend, '..');

export const ports = {
  edge: Number(process.env.E2E_EDGE_PORT ?? 8443),
  api: Number(process.env.E2E_API_PORT ?? 5080),
  ssr: Number(process.env.E2E_SSR_PORT ?? 4080),
  admin: Number(process.env.E2E_ADMIN_PORT ?? 4280),
};

const key = () => randomBytes(32).toString('base64url');

/** Starts every process; resolves once all of them accept connections. Returns a function that stops them. */
export async function startStack(logDirectory) {
  mkdirSync(logDirectory, { recursive: true });
  const keys = { apiEdge: key(), ssrIngress: key(), ssrApi: key() };
  const internalApi = `http://127.0.0.1:${ports.api}`;
  const children = [];
  const stop = async () => {
    for (const child of children) {
      child.kill();
    }
    await Promise.all(children.map((child) => (child.exitCode === null ? new Promise((done) => child.once('exit', done)) : null)));
    admin.close();
  };

  const apiDll = process.env.E2E_API_DLL ?? join(repository, 'backend', 'src', 'Api', 'bin', 'Release', 'net10.0', 'Platform.Api.dll');
  requireFile(apiDll, 'Build the API first: dotnet build backend/src/Api/Platform.Api.csproj -c Release');
  const ssrEntry = join(frontend, 'dist', 'storefront', 'server', 'server.mjs');
  requireFile(ssrEntry, 'Build the frontend first: npm run build:all');
  const adminRoot = join(frontend, 'dist', 'admin', 'browser');
  requireFile(join(adminRoot, 'index.html'), 'Build the frontend first: npm run build:all');

  // Locally every process runs on loopback, so source binding cannot be shown here (ADR-04 "Local development");
  // it is covered by the API integration tests (TH-12).
  children.push(
    run('api', 'dotnet', [apiDll], logDirectory, {
      ASPNETCORE_ENVIRONMENT: 'Testing',
      ASPNETCORE_URLS: internalApi,
      ConnectionStrings__platform:
        process.env.E2E_DATABASE ?? 'Host=127.0.0.1;Port=1;Database=platform;Username=platform_app;Password=unused;Timeout=2',
      TrustedPath__ApiEdgeKey: keys.apiEdge,
      TrustedPath__SsrApiKey: keys.ssrApi,
      TrustedPath__EdgeNetworks__0: '127.0.0.0/8',
      TrustedPath__EdgeNetworks__1: '::1/128',
      TrustedPath__SsrNetworks__0: '127.0.0.0/8',
      TrustedPath__SsrNetworks__1: '::1/128',
      TrustedPath__EnableTestDiagnostics: 'true',
    }),
    run('ssr', process.execPath, [ssrEntry], logDirectory, {
      PORT: String(ports.ssr),
      PLATFORM_SSR_INGRESS_KEY: keys.ssrIngress,
      PLATFORM_SSR_API_KEY: keys.ssrApi,
      INTERNAL_API_BASE_URL: internalApi,
      PLATFORM_ENVIRONMENT: 'Testing',
      PLATFORM_ENABLE_TEST_DIAGNOSTICS: 'true',
    }),
    run('edge', process.env.CADDY_BIN ?? 'caddy', ['run', '--config', join(repository, 'dev', 'caddy', 'Caddyfile'), '--adapter', 'caddyfile'], logDirectory, {
      PLATFORM_API_EDGE_KEY: keys.apiEdge,
      PLATFORM_SSR_INGRESS_KEY: keys.ssrIngress,
      API_UPSTREAM: internalApi,
      SSR_UPSTREAM: `http://127.0.0.1:${ports.ssr}`,
      ADMIN_UPSTREAM: `http://127.0.0.1:${ports.admin}`,
      EDGE_HTTPS_PORT: String(ports.edge),
      // Caddy's local CA and certificates stay in a throw-away directory.
      XDG_DATA_HOME: mkdtempSync(join(tmpdir(), 'platform-e2e-caddy-')),
      XDG_CONFIG_HOME: mkdtempSync(join(tmpdir(), 'platform-e2e-caddy-config-')),
    }),
  );
  const admin = await serveStatic(adminRoot, ports.admin);

  try {
    await Promise.all([
      waitForHttp(`${internalApi}/api/health`, 'api', children[0]),
      waitForHttp(`http://127.0.0.1:${ports.ssr}/`, 'ssr', children[1]),
      waitForPort(ports.edge, 'edge', children[2]),
    ]);
  } catch (error) {
    await stop();
    throw error;
  }

  return { stop, keys, internalApi };
}

function requireFile(path, hint) {
  if (!existsSync(path)) {
    throw new Error(`Missing ${path}. ${hint}`);
  }
}

/** Runs a process with a minimal environment plus its own settings; output goes to <log directory>/<name>.log. */
function run(name, command, args, logDirectory, settings) {
  const inherited = Object.fromEntries(
    ['PATH', 'Path', 'SystemRoot', 'HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP', 'DOTNET_ROOT']
      .filter((variable) => process.env[variable] !== undefined)
      .map((variable) => [variable, process.env[variable]]),
  );
  const child = spawn(command, args, { env: { ...inherited, ...settings }, stdio: ['ignore', 'pipe', 'pipe'] });
  const log = createWriteStream(join(logDirectory, `${name}.log`));
  child.stdout.pipe(log);
  child.stderr.pipe(log);
  child.on('error', (error) => log.write(`\n[stack] failed to start ${command}: ${error.message}\n`));
  return child;
}

const contentTypes = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.ico': 'image/x-icon' };

/** The admin bundle as static files (the edge forwards admin requests without a credential). */
function serveStatic(root, port) {
  const server = createServer((request, response) => {
    const path = decodeURIComponent(new URL(request.url ?? '/', 'http://admin').pathname);
    let file = normalize(join(root, path));
    if (!file.startsWith(root) || !existsSync(file) || path.endsWith('/')) {
      file = join(root, 'index.html');
    }
    response.setHeader('content-type', contentTypes[extname(file)] ?? 'application/octet-stream');
    response.end(readFileSync(file));
  });
  return new Promise((done) => server.listen(port, '127.0.0.1', () => done(server)));
}

async function waitForHttp(url, name, child) {
  await waitFor(name, child, () =>
    new Promise((done) => {
      const request = httpRequest(url, (response) => {
        response.resume();
        done(true);
      });
      request.on('error', () => done(false));
      request.end();
    }),
  );
}

async function waitForPort(port, name, child) {
  await waitFor(name, child, () =>
    new Promise((done) => {
      const socket = connect(port, '127.0.0.1', () => {
        socket.end();
        done(true);
      });
      socket.on('error', () => done(false));
    }),
  );
}

async function waitFor(name, child, ready) {
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    if (child.exitCode !== null) {
      throw new Error(`${name} stopped during startup (exit code ${child.exitCode}); see its log in test-results/stack.`);
    }
    if (await ready()) {
      return;
    }
    await new Promise((done) => setTimeout(done, 250));
  }
  throw new Error(`${name} did not start within 60 s; see its log in test-results/stack.`);
}
