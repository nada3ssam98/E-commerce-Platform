import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { ApiPath, JsonResponse } from '@platform/api-contracts';
import { map, type Observable } from 'rxjs';

const healthPath = '/api/health' satisfies ApiPath;

/** Body of `GET /api/health`, taken from the generated contract. */
export type HealthResponse = JsonResponse<typeof healthPath, 'get', 200>;

/**
 * Example of a contract-typed API call (M0 step 5; M0-13): the path and the response type come from
 * contracts/openapi.json, so a contract change that breaks this call fails the build.
 */
@Injectable({ providedIn: 'root' })
export class HealthApi {
  private readonly http = inject(HttpClient);

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(healthPath);
  }

  isHealthy(): Observable<boolean> {
    return this.getHealth().pipe(map((response) => response.status === 'Healthy'));
  }
}
