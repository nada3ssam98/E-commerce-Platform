import { Settings } from './trusted-path-headers';

export interface SsrConfig {
  /** Accepted ingress keys: the current one and, during rotation, the previous one. */
  readonly ingressKeys: readonly string[];
  readonly ssrApiKey: string;
  /** Origin of the private API, e.g. `http://api:8080`. */
  readonly internalApiBaseUrl: string;
  readonly environment: string;
  /** Test-only request-context diagnostic (ADR-04 §4d). */
  readonly testDiagnostics: boolean;
}

export type SsrConfigResult = { ok: true; config: SsrConfig } | { ok: false; errors: string[] };

const minimumKeyLength = 32;
const testingEnvironment = 'Testing';

/**
 * Reads and validates the SSR server's configuration (M0-9; ADR-04 rules 5–8, §4d).
 * Error messages name settings only — never their values.
 */
export function readSsrConfig(env: Readonly<Record<string, string | undefined>>): SsrConfigResult {
  const errors: string[] = [];
  const value = (name: string) => env[name]?.trim() || undefined;

  // TH-14: the SSR server never holds the API edge key, so any setting for it is a misconfiguration.
  for (const name of Object.keys(env)) {
    if (/API_EDGE_KEY/i.test(name) && env[name] !== undefined) {
      errors.push(`Setting '${name}' must not be provided to the SSR server (the API edge key is held by the edge only).`);
    }
  }

  const ingressKey = requiredKey(Settings.ingressKey, value(Settings.ingressKey), errors);
  const previousIngressKey = value(Settings.previousIngressKey);
  if (previousIngressKey !== undefined && previousIngressKey.length < minimumKeyLength) {
    errors.push(`Setting '${Settings.previousIngressKey}' must be at least ${minimumKeyLength} characters.`);
  }
  const ssrApiKey = requiredKey(Settings.ssrApiKey, value(Settings.ssrApiKey), errors);
  if (ingressKey !== undefined && ingressKey === ssrApiKey) {
    errors.push(`Settings '${Settings.ingressKey}' and '${Settings.ssrApiKey}' must be different credentials.`);
  }

  const internalApiBaseUrl = apiOrigin(value(Settings.internalApiBaseUrl), errors);

  const environment = value(Settings.environment) ?? 'Production';
  const diagnosticsFlag = value(Settings.testDiagnostics);
  if (diagnosticsFlag !== undefined && diagnosticsFlag !== 'true' && diagnosticsFlag !== 'false') {
    errors.push(`Setting '${Settings.testDiagnostics}' must be 'true' or 'false'.`);
  }
  const testDiagnostics = diagnosticsFlag === 'true';
  if (testDiagnostics && environment !== testingEnvironment) {
    errors.push(`Setting '${Settings.testDiagnostics}' is allowed only when '${Settings.environment}' is '${testingEnvironment}'.`);
  }

  if (errors.length > 0 || ingressKey === undefined || ssrApiKey === undefined || internalApiBaseUrl === undefined) {
    return { ok: false, errors };
  }

  return {
    ok: true,
    config: {
      ingressKeys: previousIngressKey === undefined ? [ingressKey] : [ingressKey, previousIngressKey],
      ssrApiKey,
      internalApiBaseUrl,
      environment,
      testDiagnostics,
    },
  };
}

function requiredKey(name: string, key: string | undefined, errors: string[]): string | undefined {
  if (key === undefined) {
    errors.push(`Missing setting '${name}'.`);
    return undefined;
  }
  if (key.length < minimumKeyLength) {
    errors.push(`Setting '${name}' must be at least ${minimumKeyLength} characters.`);
    return undefined;
  }
  return key;
}

function apiOrigin(raw: string | undefined, errors: string[]): string | undefined {
  const name = Settings.internalApiBaseUrl;
  if (raw === undefined) {
    errors.push(`Missing setting '${name}'.`);
    return undefined;
  }
  let url: URL;
  try {
    url = new URL(raw);
  } catch {
    errors.push(`Setting '${name}' is not a valid URL.`);
    return undefined;
  }
  if ((url.protocol !== 'http:' && url.protocol !== 'https:') || url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
    errors.push(`Setting '${name}' must be an http(s) origin without credentials, path, or query.`);
    return undefined;
  }
  return url.origin;
}
