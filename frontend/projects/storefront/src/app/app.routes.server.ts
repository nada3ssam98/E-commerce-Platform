import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Storefront pages are rendered on the server per request, never prerendered: their content depends on the
 * store resolved from the request's host (requirements §24; implementation-plan §9.2).
 * Client-rendered routes (cart, checkout, account) are added with their milestones.
 */
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Server,
  },
];
