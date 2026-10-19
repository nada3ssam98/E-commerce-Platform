/**
 * Per-request values the SSR server passes to Angular through `REQUEST_CONTEXT` (server only; never serialized
 * to the browser). Absent when the app runs under the Angular CLI dev server without the trusted path.
 */
export interface SsrRequestContext {
  /** Public host, taken from the verified `X-Platform-Host` (ADR-04 rule 6). */
  readonly platformHost: string;
  /** Origin of the private API (`INTERNAL_API_BASE_URL`). */
  readonly internalApiBaseUrl: string;
  /** SSR → API credential (`PLATFORM_SSR_API_KEY`). */
  readonly ssrApiKey: string;
}
