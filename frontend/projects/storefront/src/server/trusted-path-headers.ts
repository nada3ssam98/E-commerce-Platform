/**
 * Trusted-path header and setting names (ADR-04 §4b). Server-only: never imported by browser code (TH-8).
 */
export const Headers = {
  /** Set by the edge from the host it received; the only source of the public host (ADR-04 rule 6). */
  platformHost: 'x-platform-host',
  /** Edge → SSR credential, verified before rendering (rule 5). */
  ssrIngressKey: 'x-platform-ssr-ingress-key',
  /** SSR → API credential, limited to allow-listed public GETs (rule 7). */
  ssrApiKey: 'x-platform-ssr-api-key',
  /** Edge → API credential. The SSR server never holds it; it is only reported by the test diagnostic. */
  apiEdgeKey: 'x-platform-api-edge-key',
} as const;

/** Environment variables of the SSR server (rule 8: server environment only). */
export const Settings = {
  ingressKey: 'PLATFORM_SSR_INGRESS_KEY',
  /** Optional second active value during rotation (ADR-04 §4b). */
  previousIngressKey: 'PLATFORM_SSR_INGRESS_KEY_PREVIOUS',
  ssrApiKey: 'PLATFORM_SSR_API_KEY',
  internalApiBaseUrl: 'INTERNAL_API_BASE_URL',
  environment: 'PLATFORM_ENVIRONMENT',
  testDiagnostics: 'PLATFORM_ENABLE_TEST_DIAGNOSTICS',
} as const;
