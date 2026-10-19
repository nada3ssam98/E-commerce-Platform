import { HTTP_INTERCEPTORS, HttpClient, HttpHeaders, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { REQUEST_CONTEXT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import type { SsrRequestContext } from '../ssr/ssr-request-context';
import { ServerApiInterceptor } from './server-api-interceptor';

const context: SsrRequestContext = {
  platformHost: 'brand-a.localhost',
  internalApiBaseUrl: 'http://api.internal:8080',
  ssrApiKey: 'ssr-api-key-0123456789abcdef0123456789',
};

function setUp(requestContext: SsrRequestContext | null) {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptorsFromDi()),
      provideHttpClientTesting(),
      { provide: HTTP_INTERCEPTORS, useClass: ServerApiInterceptor, multi: true },
      ...(requestContext ? [{ provide: REQUEST_CONTEXT, useValue: requestContext }] : []),
    ],
  });
  return { http: TestBed.inject(HttpClient), backend: TestBed.inject(HttpTestingController) };
}

describe('ServerApiInterceptor', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('sends API GETs to the internal API with the verified host and the SSR-to-API key only (TH-4)', () => {
    const { http, backend } = setUp(context);

    void firstValueFrom(
      http.get('/api/health', {
        headers: new HttpHeaders({
          Cookie: '__Host-sf_session=browser-cookie',
          Authorization: 'Bearer token',
          'X-Platform-Api-Edge-Key': 'forged',
          'X-Platform-Ssr-Ingress-Key': 'forged',
        }),
      }),
    );

    const request = backend.expectOne('http://api.internal:8080/api/health');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(false);
    expect(request.request.headers.keys().sort()).toEqual(['Accept', 'x-platform-host', 'x-platform-ssr-api-key']);
    expect(request.request.headers.get('x-platform-host')).toBe('brand-a.localhost');
    expect(request.request.headers.get('x-platform-ssr-api-key')).toBe(context.ssrApiKey);
    request.flush({ status: 'Healthy' });
  });

  it.each(['POST', 'PUT', 'PATCH', 'DELETE'])('never sends %s to the API', async (method) => {
    const { http, backend } = setUp(context);

    await expect(firstValueFrom(http.request(method, '/api/cart/lines', { body: {} }))).rejects.toThrow(/GET only/);
    backend.expectNone(() => true);
  });

  it('leaves non-API requests unchanged', () => {
    const { http, backend } = setUp(context);

    void firstValueFrom(http.get('/assets/x.json'));

    const request = backend.expectOne('/assets/x.json');
    expect(request.request.headers.has('x-platform-ssr-api-key')).toBe(false);
    request.flush({});
  });

  it('does nothing without a request context (Angular CLI dev server)', () => {
    const { http, backend } = setUp(null);

    void firstValueFrom(http.get('/api/health'));

    const request = backend.expectOne('/api/health');
    expect(request.request.headers.has('x-platform-ssr-api-key')).toBe(false);
    request.flush({ status: 'Healthy' });
  });
});
