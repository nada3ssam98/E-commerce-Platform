import type { NextFunction, Request, Response } from 'express';
import { createIngressGuard } from './ingress-guard';

const key = 'ingress-key-0123456789abcdef0123456789';

function run(headers: Record<string, string | string[]>) {
  const logged: Record<string, string>[] = [];
  const request = { headers: { ...headers }, method: 'GET', path: '/ar/' } as unknown as Request;
  const sent: { status?: number; body?: string; cacheControl?: string } = {};
  const response = {
    locals: {} as Record<string, unknown>,
    status(code: number) {
      sent.status = code;
      return this;
    },
    set(name: string, value: string) {
      if (name === 'Cache-Control') sent.cacheControl = value;
      return this;
    },
    type() {
      return this;
    },
    send(body: string) {
      sent.body = body;
      return this;
    },
  } as unknown as Response & { locals: Record<string, unknown> };
  const next = vi.fn() as unknown as NextFunction;

  createIngressGuard([key], (entry) => logged.push(entry))(request, response, next);
  return { request, response, next, sent, logged };
}

describe('createIngressGuard', () => {
  it('passes a request with the ingress key and a valid platform host', () => {
    const { response, next, sent } = run({ 'x-platform-ssr-ingress-key': key, 'x-platform-host': 'Brand-A.localhost:443' });

    expect(next).toHaveBeenCalledOnce();
    expect(sent.status).toBeUndefined();
    expect(response.locals['platformHost']).toBe('brand-a.localhost');
  });

  it.each([
    ['no key', { 'x-platform-host': 'brand-a.localhost' }, 'missing-ingress-key'],
    ['wrong key', { 'x-platform-ssr-ingress-key': 'wrong', 'x-platform-host': 'brand-a.localhost' }, 'invalid-ingress-key'],
    ['duplicated key header', { 'x-platform-ssr-ingress-key': [key, key], 'x-platform-host': 'brand-a.localhost' }, 'invalid-ingress-key'],
    ['no platform host', { 'x-platform-ssr-ingress-key': key }, 'missing-or-invalid-platform-host'],
    ['invalid platform host', { 'x-platform-ssr-ingress-key': key, 'x-platform-host': 'brand a' }, 'missing-or-invalid-platform-host'],
  ])('rejects %s with 403 and logs no secret (TH-3)', (_case, headers, reason) => {
    const { next, sent, logged } = run(headers);

    expect(next).not.toHaveBeenCalled();
    expect(sent).toEqual({ status: 403, body: 'Forbidden', cacheControl: 'no-store' });
    expect(logged).toEqual([{ message: 'Rejected request', reason, method: 'GET', path: '/ar/' }]);
    expect(JSON.stringify(logged)).not.toContain(key);
  });

  it('drops client-supplied forwarded-host headers (ADR-04 rule 6)', () => {
    const { request } = run({
      'x-platform-ssr-ingress-key': key,
      'x-platform-host': 'brand-a.localhost',
      'x-forwarded-host': 'brand-b.localhost',
      forwarded: 'host=brand-b.localhost',
    });

    expect(request.headers['x-forwarded-host']).toBeUndefined();
    expect(request.headers['forwarded']).toBeUndefined();
  });
});
