import { HttpEvent, HttpHandler, HttpHeaders, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { inject, Injectable, REQUEST_CONTEXT } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { Headers } from '../../../server/trusted-path-headers';
import type { SsrRequestContext } from '../ssr/ssr-request-context';

/**
 * Server-only (registered in app.config.server.ts, never in the browser bundle — ADR-04 rule 7; TH-8):
 * relative `/api/...` calls made while rendering go to the private API with the verified host and the
 * SSR-to-API key, `GET` only. All outgoing headers are replaced, so no browser cookie, `Authorization`, or
 * incoming `X-Platform-*` value is forwarded.
 */
@Injectable()
export class ServerApiInterceptor implements HttpInterceptor {
  private readonly context = inject(REQUEST_CONTEXT, { optional: true }) as SsrRequestContext | null;

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!request.url.startsWith('/api/') || this.context === null) {
      // Not an API call, or the Angular CLI dev server without the trusted path.
      return next.handle(request);
    }
    if (request.method !== 'GET') {
      return throwError(() => new Error(`The SSR server may call the API with GET only (${request.method} ${request.url}).`));
    }

    return next.handle(
      request.clone({
        url: new URL(request.url, this.context.internalApiBaseUrl).toString(),
        headers: new HttpHeaders({
          Accept: request.headers.get('Accept') ?? 'application/json',
          [Headers.platformHost]: this.context.platformHost,
          [Headers.ssrApiKey]: this.context.ssrApiKey,
        }),
        withCredentials: false,
      }),
    );
  }
}
