import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApplicationConfig, mergeApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { ServerApiInterceptor } from './core/api/server-api-interceptor';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    // Server only (ADR-04 rule 7): never registered in the browser application.
    { provide: HTTP_INTERCEPTORS, useClass: ServerApiInterceptor, multi: true },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
