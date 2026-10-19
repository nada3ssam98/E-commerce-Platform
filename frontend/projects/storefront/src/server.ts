import { AngularNodeAppEngine, createNodeRequestHandler, isMainModule, writeResponseToNodeResponse } from '@angular/ssr/node';
import express from 'express';
import type { AddressInfo } from 'node:net';
import { join } from 'node:path';
import type { SsrRequestContext } from './app/core/ssr/ssr-request-context';
import { createIngressGuard } from './server/ingress-guard';
import { readSsrConfig, type SsrConfig } from './server/ssr-config';
import { Headers } from './server/trusted-path-headers';

const browserDistFolder = join(import.meta.dirname, '../browser');
const runsAsServer = isMainModule(import.meta.url) || Boolean(process.env['pm_id']);

/**
 * Configuration comes from the server environment only (ADR-04 rule 8). Running as the server, invalid or
 * missing configuration stops startup with the setting names and no values (M0-9, TH-9, TH-14). When the module
 * is only loaded by the Angular CLI (dev server, build), the trusted path is active only if configured.
 */
const configResult = readSsrConfig(process.env);
if (runsAsServer && !configResult.ok) {
  console.error(['SSR server configuration is invalid:', ...configResult.errors.map((error) => `  - ${error}`)].join('\n'));
  process.exit(1);
}
const config: SsrConfig | undefined = configResult.ok ? configResult.config : undefined;
if (!config) {
  console.warn('SSR trusted path disabled: no valid configuration (Angular CLI dev server or build only).');
}

const app = express();
app.disable('x-powered-by');
const angularApp = new AngularNodeAppEngine();

if (config) {
  // Rule 5: nothing is served or rendered before the ingress key and the platform host are verified.
  app.use(createIngressGuard(config.ingressKeys));

  if (config.testDiagnostics) {
    // ADR-04 §4d, test-only (environment Testing + explicit flag): which credential headers arrived (TH-13).
    app.get('/_test/ssr-request-context', (request, response) => {
      response.set('Cache-Control', 'no-store').json({
        platformHost: response.locals['platformHost'],
        credentialHeaders: {
          [Headers.ssrIngressKey]: request.headers[Headers.ssrIngressKey] !== undefined,
          [Headers.apiEdgeKey]: request.headers[Headers.apiEdgeKey] !== undefined,
          [Headers.ssrApiKey]: request.headers[Headers.ssrApiKey] !== undefined,
        },
      });
    });
  } else {
    // TH-9: the diagnostic does not exist outside Testing.
    app.use('/_test', (_request, response) => {
      response.status(404).set('Cache-Control', 'no-store').end();
    });
  }
}

app.use(express.static(browserDistFolder, { maxAge: '1y', index: false, redirect: false }));

app.use((request, response, next) => {
  const context: SsrRequestContext | undefined = config && {
    platformHost: response.locals['platformHost'] as string,
    internalApiBaseUrl: config.internalApiBaseUrl,
    ssrApiKey: config.ssrApiKey,
  };
  angularApp
    .handle(request, context)
    .then((rendered) => (rendered ? writeResponseToNodeResponse(rendered, response) : next()))
    .catch(next);
});

if (runsAsServer) {
  const server = app.listen(Number(process.env['PORT'] ?? 4000), (error) => {
    if (error) {
      throw error;
    }
    console.log(`Node Express server listening on http://localhost:${(server.address() as AddressInfo).port}`);
  });
}

/** Request handler used by the Angular CLI (dev server and build). */
export const reqHandler = createNodeRequestHandler(app);
