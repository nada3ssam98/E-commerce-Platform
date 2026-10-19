import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { HealthApi } from './core/api/health-api';

/**
 * M0 placeholder page of the public storefront (server-rendered). It shows the API status fetched during
 * server rendering through the SSR server's internal API call (M0-2). Store resolution, status pages, and the
 * shell arrive in M1.
 */
@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  template: `
    <main>
      <h1 i18n="Storefront placeholder heading@@storefront.placeholder.heading">Welcome</h1>
      <p i18n="Storefront placeholder text@@storefront.placeholder.text">This page is a placeholder for the storefront.</p>
      <p>
        <span i18n="Label before the API health status@@storefront.placeholder.apiStatus">API status:</span>
        <span data-testid="api-health">{{ apiStatus() ?? unavailable }}</span>
      </p>
    </main>
    <router-outlet />
  `,
})
export class App {
  protected readonly unavailable = $localize`:Shown when the API status could not be read@@storefront.placeholder.apiUnavailable:unavailable`;

  protected readonly apiStatus = toSignal(
    inject(HealthApi)
      .getHealth()
      .pipe(
        map((response) => response.status),
        catchError(() => of(null)),
      ),
    { initialValue: null },
  );
}
