# Frontend

Angular CLI workspace (ADR-03; requirements §24): two applications and shared libraries.

| Project | Kind | Notes |
|---|---|---|
| `storefront` | Application, **SSR** | Public store pages, rendered on the server per request (never prerendered). One build per locale, served under `/en/` and `/ar/` (T-1). |
| `admin` | Application, **client-only** | Store administration. Locales are configured; how the admin selects its locale build is decided with the admin shell (M2). |
| `@platform/i18n` | Library | Locale and direction (`provideHtmlLangAndDir()` sets `<html lang dir>`); formatting helpers follow. |
| `@platform/design-system` | Library | Empty until the Phase 5 UI design (`docs/ui-design.md`) is approved. |

## Toolchain

Node.js **24.15.0** and npm **11.12.1** exactly (`.nvmrc`, `engines`, `engine-strict`). Angular **22.2.3**, TypeScript **6.0.3**; every dependency is pinned exactly and installed from `package-lock.json`.

```bash
npm ci                    # reproducible install (fails on a different Node/npm or an out-of-sync lock file)
npm run build:all         # production builds: storefront (en + ar, SSR) and admin
npm run test:all          # unit tests (Vitest, Angular CLI default)
npm run serve:ssr:storefront   # after build: http://localhost:4000/en/ and /ar/
npx ng serve storefront   # dev server, English; Arabic: npx ng serve storefront -c development-ar
npx ng extract-i18n storefront # refresh projects/storefront/src/locale/messages.xlf, then update messages.ar.xlf
```

The SSR server accepts only allow-listed hosts (`localhost`, `*.localhost` for local development — ADR-04 rule 6).
