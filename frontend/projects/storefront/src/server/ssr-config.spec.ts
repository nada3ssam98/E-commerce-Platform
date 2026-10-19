import { readSsrConfig } from './ssr-config';

const ingressKey = 'ingress-key-0123456789abcdef0123456789';
const ssrApiKey = 'ssr-api-key-0123456789abcdef0123456789';
const valid = {
  PLATFORM_SSR_INGRESS_KEY: ingressKey,
  PLATFORM_SSR_API_KEY: ssrApiKey,
  INTERNAL_API_BASE_URL: 'http://api.internal:8080',
};

function errorsFor(env: Record<string, string | undefined>): string[] {
  const result = readSsrConfig(env);
  if (result.ok) {
    throw new Error('expected invalid configuration');
  }
  return result.errors;
}

describe('readSsrConfig', () => {
  it('accepts a complete configuration; Production is the default environment', () => {
    const result = readSsrConfig(valid);

    expect(result).toEqual({
      ok: true,
      config: {
        ingressKeys: [ingressKey],
        ssrApiKey,
        internalApiBaseUrl: 'http://api.internal:8080',
        environment: 'Production',
        testDiagnostics: false,
      },
    });
  });

  it('accepts a previous ingress key during rotation', () => {
    const previous = 'previous-ingress-key-0123456789abcdef';
    const result = readSsrConfig({ ...valid, PLATFORM_SSR_INGRESS_KEY_PREVIOUS: previous });

    expect(result.ok && result.config.ingressKeys).toEqual([ingressKey, previous]);
  });

  it('names every missing setting (M0-9)', () => {
    expect(errorsFor({})).toEqual([
      "Missing setting 'PLATFORM_SSR_INGRESS_KEY'.",
      "Missing setting 'PLATFORM_SSR_API_KEY'.",
      "Missing setting 'INTERNAL_API_BASE_URL'.",
    ]);
  });

  it('rejects weak or reused keys without printing them', () => {
    const errors = errorsFor({ ...valid, PLATFORM_SSR_INGRESS_KEY: 'short-secret', PLATFORM_SSR_API_KEY: ingressKey });

    expect(errors.join('\n')).toContain("'PLATFORM_SSR_INGRESS_KEY' must be at least 32 characters");
    expect(errors.join('\n')).not.toContain('short-secret');
    expect(errors.join('\n')).not.toContain(ingressKey);
  });

  it('rejects the same value for both credentials', () => {
    expect(errorsFor({ ...valid, PLATFORM_SSR_API_KEY: ingressKey })).toContain(
      "Settings 'PLATFORM_SSR_INGRESS_KEY' and 'PLATFORM_SSR_API_KEY' must be different credentials.",
    );
  });

  it.each(['not a url', 'ftp://api.internal', 'http://user:secret@api.internal', 'http://api.internal/v1', 'http://api.internal/?x=1'])(
    'rejects INTERNAL_API_BASE_URL %s',
    (url) => {
      expect(errorsFor({ ...valid, INTERNAL_API_BASE_URL: url }).join('\n')).toContain("'INTERNAL_API_BASE_URL'");
    },
  );

  it.each(['PLATFORM_API_EDGE_KEY', 'platform_api_edge_key'])('rejects an API edge key setting: %s (TH-14)', (name) => {
    expect(errorsFor({ ...valid, [name]: 'any-value' })).toEqual([
      `Setting '${name}' must not be provided to the SSR server (the API edge key is held by the edge only).`,
    ]);
  });

  it('allows the test diagnostic only in the Testing environment (TH-9)', () => {
    expect(errorsFor({ ...valid, PLATFORM_ENABLE_TEST_DIAGNOSTICS: 'true' })).toEqual([
      "Setting 'PLATFORM_ENABLE_TEST_DIAGNOSTICS' is allowed only when 'PLATFORM_ENVIRONMENT' is 'Testing'.",
    ]);

    const testing = readSsrConfig({ ...valid, PLATFORM_ENVIRONMENT: 'Testing', PLATFORM_ENABLE_TEST_DIAGNOSTICS: 'true' });
    expect(testing.ok && testing.config.testDiagnostics).toBe(true);
  });
});
