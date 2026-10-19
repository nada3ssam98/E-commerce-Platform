import { provideHttpClient, withFetch, withInterceptorsFromDi } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideClientHydration } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHtmlLangAndDir } from '@platform/i18n';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(),
    // DI interceptors: the server application adds the SSR-to-API interceptor (app.config.server.ts).
    provideHttpClient(withFetch(), withInterceptorsFromDi()),
    provideHtmlLangAndDir(),
  ],
};
