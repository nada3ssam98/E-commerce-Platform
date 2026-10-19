import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { HealthApi, type HealthResponse } from './health-api';

describe('HealthApi', () => {
  let api: HealthApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(HealthApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests GET /api/health and returns the contract-typed body', async () => {
    const response = firstValueFrom(api.getHealth());

    const request = http.expectOne('/api/health');
    expect(request.request.method).toBe('GET');
    request.flush({ status: 'Healthy' } satisfies HealthResponse);

    expect(await response).toEqual({ status: 'Healthy' });
  });

  it.each([
    ['Healthy', true],
    ['Degraded', false],
    ['Unhealthy', false],
  ] as const)('isHealthy() is %s → %s', async (status, expected) => {
    const healthy = firstValueFrom(api.isHealthy());

    http.expectOne('/api/health').flush({ status } satisfies HealthResponse);

    expect(await healthy).toBe(expected);
  });

  it('types the body from the generated contract', () => {
    expectTypeOf<HealthResponse>().toEqualTypeOf<{ status: 'Healthy' | 'Degraded' | 'Unhealthy' }>();
  });
});
