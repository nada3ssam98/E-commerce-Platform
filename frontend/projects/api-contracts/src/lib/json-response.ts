import type { paths } from './schema';

/** A path that exists in the API contract. */
export type ApiPath = keyof paths;

/**
 * The `application/json` body of a response in the contract, e.g. `JsonResponse<'/api/health', 'get', 200>`.
 * Resolves to `never` when the path, method, or status is not in the contract.
 */
export type JsonResponse<P extends ApiPath, M extends keyof paths[P], S extends number> = paths[P][M] extends {
  responses: infer Responses;
}
  ? S extends keyof Responses
    ? Responses[S] extends { content: { 'application/json': infer Body } }
      ? Body
      : never
    : never
  : never;
