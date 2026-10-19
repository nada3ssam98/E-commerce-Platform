import type { NextFunction, Request, Response } from 'express';
import { normalizeHost } from './host';
import { matchesAnyKey } from './key-match';
import { Headers } from './trusted-path-headers';

export type SecurityLog = (entry: Record<string, string>) => void;

/** One JSON line per rejected request; never contains header values. */
export const consoleSecurityLog: SecurityLog = (entry) =>
  console.warn(JSON.stringify({ level: 'Warning', category: 'Platform.Ssr.TrustedPath', ...entry }));

/**
 * Runs before anything is served or rendered (ADR-04 rules 5 and 6):
 * - the SSR ingress key must match (constant time), otherwise `403`, nothing rendered, security log entry;
 * - the public host is taken only from the verified `X-Platform-Host`; forwarded-host headers are dropped so
 *   nothing downstream can read a client-supplied host.
 * On success the verified host is available as `res.locals['platformHost']`.
 */
export function createIngressGuard(ingressKeys: readonly string[], log: SecurityLog = consoleSecurityLog) {
  return (request: Request, response: Response, next: NextFunction): void => {
    const presented = request.headers[Headers.ssrIngressKey];
    if (typeof presented !== 'string' || !matchesAnyKey(presented, ingressKeys)) {
      reject(request, response, log, presented === undefined ? 'missing-ingress-key' : 'invalid-ingress-key');
      return;
    }

    const host = normalizeHost(single(request.headers[Headers.platformHost]));
    if (host === null) {
      reject(request, response, log, 'missing-or-invalid-platform-host');
      return;
    }

    delete request.headers['x-forwarded-host'];
    delete request.headers['forwarded'];
    response.locals['platformHost'] = host;
    next();
  };
}

function single(value: string | string[] | undefined): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

function reject(request: Request, response: Response, log: SecurityLog, reason: string): void {
  log({ message: 'Rejected request', reason, method: request.method, path: request.path });
  response.status(403).set('Cache-Control', 'no-store').type('text/plain').send('Forbidden');
}
