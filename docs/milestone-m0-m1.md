# Milestones M0 + M1 — Scope and Acceptance Criteria

| | |
|---|---|
| **Status** | **Draft — not approved; implementation not started** |
| **Last updated** | 2026-10-18 (architecture-review revision) |
| **Depends on** | Approval of [architecture-decisions.md](architecture-decisions.md) revision 3 (ADR-01–04, ADR-05 password hashing, ADR-11, X) and of the v1.9 items that touch M0/M1 (see §1) |

> **Goal.** Leave the repository with a working, tested foundation on which every later milestone is built as an end-to-end vertical slice: one codebase, a real database, CI, a **proven trusted-host path**, and **correct, tested tenant resolution** — before any commerce data exists.
>
> Where this document names a tool or version, it refers to the **proposed** ADRs. If an ADR changes during review, the deliverable stays and the tool changes.
>
> **Not in M0/M1:** catalog, media, inventory, cart, checkout, orders, payments, staff login, customer accounts.

---

## 1. Prerequisites (before M0 starts)

| # | Prerequisite | Why |
|---|---|---|
| P1 | ADR-01, 02, 03, 04, 11 and the cross-cutting conventions approved | M0 builds exactly these |
| P2 | ADR-05 **password hashing** part approved | M1 provisioning stores a temporary password hash |
| P3 | RA-1, RA-2, RA-3 approved | Operator commands (RA-1, RA-2) and storefront routing (RA-3) |
| P4 | v1.9 items C1 (TTL-only caching) and A4 (generated temporary passwords only) approved | M1 implements resolution caching and provisioning |
| P5 | Decisions M1-D1 … M1-D5 and U-3 (`Retry-After` value) | Small scope choices inside M1 |
| P6 | Toolchain baseline re-checked at M0 start (Node 26 LTS / Angular 23 availability — ADR-11) | Pins are recorded once, at scaffolding |

**Not required before M0/M1:** ADR-06–10, ADR-12, RA-4–RA-8 (RA-7 is needed from M4, RA-8 from M5/M7), C2, C3, A1–A3, A5, D2.

---

## 2. M0 — Walking skeleton

**Goal:** prove the chosen stack end to end — including the trusted-host path — with no business features.

### Scope

| Area | Deliverables |
|---|---|
| Repository | Layout per ADR-03: `/frontend` (Angular CLI workspace — `storefront` with SSR, `admin` client-only; libraries for design-system foundation, i18n/formatting, and generated `api-contracts`), `/backend` (.NET solution), `/contracts`, `/dev` |
| Backend | .NET 10 solution (`.slnx`, central package management, nullable on, warnings as errors): `Api` host (ASP.NET Core Minimal APIs), `Cli` host (console app, no commands yet), empty domain projects with the **allowed project-reference graph** from system-behavior §1, `Infrastructure`. API provides: options validation on start (fail fast), JSON logging with compliance redaction, Problem Details error format with `code` (ADR X), request id, OpenTelemetry wiring, `GET /api/health` (liveness) and `GET /api/health/ready` (database reachable). Default response header `Cache-Control: private, no-store` unless an endpoint opts in to public caching. The `Worker` host is added in M2. |
| Database | PostgreSQL (local container); EF Core 10 + Npgsql; an initial migration; CI applies migrations from scratch; separate migration and application database roles |
| **Trusted-host path** (ADR-04) | Local edge (Caddy) implementing edge rules 1–4: deletes client host-override headers, sets `X-Platform-Host` and the edge key. SSR server: edge-key verification before rendering; server-only interceptor calling the API on `INTERNAL_API_BASE_URL` with the verified host and the SSR key, forwarding no cookies. API: trusted-path middleware (edge key or SSR key), `X-Platform-Host` as the only host source, SSR-key route allow-list (in M0: `GET /api/health*`), Forwarded Headers never processing `X-Forwarded-Host`. Test-only request-context diagnostic (ADR-04 §4c). Store resolution itself comes in M1. |
| Contracts | `contracts/openapi.json` generated at build (`Microsoft.Extensions.ApiDescription.Server`); **`openapi-typescript` 7.13.0** (exact pin) via `npm run contracts:generate` = `openapi-typescript ../contracts/openapi.json --output projects/api-contracts/src/lib/schema.d.ts`; one example endpoint typed end to end; shared test-vector file location established |
| Frontend | Storefront SSR renders one page that calls `/api/health` server-side; admin builds and serves a placeholder page; `<html lang dir>` wiring present |
| Local environment | Aspire AppHost (or Docker Compose, per ADR-11) starting PostgreSQL, the API, and the Angular apps; Caddy serving `https://*.localhost` and `https://admin.localhost` with trusted local certificates |
| **Reproducible installs** | `global.json` (.NET 10 SDK, `rollForward: latestPatch`); `RestorePackagesWithLockFile=true` + committed `packages.lock.json`; `.nvmrc` (Node 24 LTS ≥ 24.13.1, unless P6 moves to Node 26) + `engines`; `.npmrc` with `save-exact=true`; committed `package-lock.json`; Angular 22.x, Playwright 1.62.x, openapi-typescript 7.13.0 pinned exactly |
| Configuration hygiene | `dotnet user-secrets` for local backend secrets (including the edge and SSR keys); `.env.example` / `appsettings.Development.json` with placeholders only; `.gitignore` covering secrets, `bin/`, `obj/`, `node_modules/`, build output; `.gitattributes` normalizing line endings |
| CI | GitHub Actions on every push and pull request: `dotnet restore --locked-mode` / build / test; migrations from scratch; **OpenAPI drift check** (`dotnet build` → `npm run contracts:generate` → `git diff --exit-code -- contracts/ frontend/projects/api-contracts/`); `npm ci`, lint, test, production builds; **browser-bundle scan** for the SSR key and internal API URL (TH-8); secret scan; dependency audit (`dotnet list package --vulnerable`, `npm audit`); E2E through the local edge |
| Test harness | xUnit v3 projects; `WebApplicationFactory` + Testcontainers for .NET (PostgreSQL) + Respawn helper; architecture test project (ArchUnitNET or NetArchTest) asserting the domain reference graph; Playwright (TypeScript) running through the local edge; Angular unit test runner configured |
| Documentation | `README.md`: prerequisites (.NET 10 SDK, Node.js per `.nvmrc`, container runtime, trusting the local certificate), one-command local start, how to run each test layer, and the **manual branch-protection setup** (below) |

### Out of scope
Any business table, authentication, store resolution, styling beyond a placeholder, deployment.

### Acceptance criteria — M0

| # | Criterion | How verified |
|---|---|---|
| M0-1 | A fresh clone starts the full local stack with the documented commands. | Manual check by a second person following README only |
| M0-2 | `https://brand-a.localhost/` renders a server-side page whose initial HTML contains a value fetched from `/api/health` **through the SSR server's internal call**. | Playwright inspecting the initial HTML |
| M0-3 | `https://admin.localhost/` serves the admin placeholder; `/api/health` is reachable same-origin on both hosts. | Playwright |
| M0-4 | **TH-1:** a browser request with forged `X-Forwarded-Host`, `Forwarded`, and `X-Platform-Host` headers reaches the API with the **edge-set** `X-Platform-Host`. | E2E through the edge, asserting via the test-only diagnostic |
| M0-5 | **TH-2 / TH-3:** direct requests to the API or the SSR server that bypass the edge (no key, wrong key, forged host header) are rejected with `403`; the SSR server renders nothing and makes no API call. | Integration tests against the API and SSR ports |
| M0-6 | **TH-4 / TH-5:** the SSR server's internal API call carries the verified host and the SSR key; the SSR key used on a `POST` or a non-allow-listed route is rejected with `403`. | E2E + API integration tests |
| M0-7 | **TH-8:** browser bundles contain neither the SSR key nor the internal API URL. | CI bundle scan |
| M0-8 | **TH-9:** the request-context diagnostic returns `404` when the API runs as `Production`; startup fails if the diagnostic flag is set outside `Testing`. | Integration test starting the host in each configuration |
| M0-9 | The API refuses to start with missing or invalid configuration (including missing keys) and names the missing setting without printing secret values. | Test of options validation on start |
| M0-10 | Logs redact cookies, `Authorization`, the edge/SSR keys, and data classified as tokens, passwords, phones, emails. | Unit test on the logging/redaction configuration |
| M0-11 | Migrations run from zero on an empty database in CI; the application role cannot run DDL. | CI integration job |
| M0-12 | A project reference that violates the domain dependency graph (e.g., `Catalog` → `Ordering`) fails the build or the architecture test. | Architecture test with a deliberate violation fixture |
| M0-13 | Changing an endpoint without regenerating `openapi.json` and the TypeScript types fails CI; the Angular example call compiles only against the generated types. | CI drift check |
| M0-14 | Installs are reproducible: CI uses `dotnet restore --locked-mode` and `npm ci`; a dependency change without an updated lockfile fails CI; tool versions match the baseline files. | CI |
| M0-15 | An endpoint that does not opt in to public caching returns `Cache-Control: private, no-store`. | Integration test |
| M0-16 | No secrets in the repository; placeholders only in example configuration. | CI secret scan + review |

**Manual repository configuration (not provable by CI):** the repository owner configures branch protection on `main` — pull request required, the CI checks above required to pass, no direct pushes. CI can only show that checks run and pass; it cannot prove that GitHub enforces them. **Verified by:** the owner reviews the branch-protection settings and records the date in the README. *(Requires the owner's GitHub access.)*

---

## 3. M1 — Tenancy core

**Goal:** every request to a store host is resolved — through the trusted path only — to exactly one store (or to none), with the effective status applied, before any commerce feature exists; the isolation test harness is in place.

### Scope

**Database (migrations)**

| Table | Notes |
|---|---|
| `organizations` | `status` `Active`/`Suspended` |
| `stores` | `handle` (unique, format, reserved words, immutable after activation), `brand_name`, `display_name` (LocalizedText), `status`, `activated_at` |
| `store_domains` | `hostname` unique platform-wide; exactly one primary per store |
| `store_settings` | All groups in domain-model §1.5 with defaults from W1; `version` |
| `staff_users`, `staff_password_credentials`, `store_memberships` | Created by provisioning only; **no login in M1** (M2) |
| `platform_audit_events` | Append-only; application role has no `UPDATE`/`DELETE` on it |
| `order_number_sequences` | See M1-D2 |
| `store_branding` | See M1-D1 |
| Reference data | Platform limits (line max 10, cart max 50 lines, image max) as configuration |

**Backend**

| Deliverable | Detail |
|---|---|
| Host normalization | Lowercase, strip port, strip trailing dot, validate syntax — from `X-Platform-Host` only, accepted only with a valid edge or SSR key (ADR-04) |
| Store resolver | Host → StoreDomain → Store; **unknown host → `STORE_NOT_FOUND`, never a fallback**; in-process cache ≤ 60 s used for rendering decisions only (C1) |
| Effective status | Organization `Suspended` ⇒ store behaves as `Suspended`. Operator commands read the **current** status from the database, never the cache. |
| `GET /api/storefront/config` | Storefront view per domain-model §1.9: brand name, resolved display name + source locale, effective status, supported/default locales, currency, `pricesIncludeTax`, timezone, shipping rule, support contacts, platform limits. **Excludes** `organizationId`, `lowStockThreshold`, `version`. Added to the SSR-key allow-list. |
| Cache headers | Config and coming-soon: `public, max-age=0, s-maxage=60`, no `Set-Cookie`. Suspended `503`: `no-store` + `Retry-After` (U-3: 3600 *[DEFAULT]*). Errors (404, 5xx): `no-store`. Everything else: `private, no-store` (M0 default). |
| Operator CLI commands (`Cli` host) | `provision`, `activate`, `suspend-store`, `reactivate-store`, `suspend-organization`, `reactivate-organization`. Each requires an **operator identifier** (RA-1 — recorded as a self-declared audit label, not verified identity) and writes PlatformAuditEvents in the same transaction. Suspension commands require a reason. |
| Password hashing | Argon2id library **vetted** per ADR-05 (maintenance, official test vectors, PHC encoding, timing); bcrypt (cost ≥ 12) if none passes. Outcome recorded in the ADR. |
| Provisioning | Exactly W1: validation; staff lookup (new / reuse `Active` / refuse `Disabled` per RA-2); one transaction; temporary password **generated** with a secure generator (never operator-supplied — A4), hashed, printed **once after commit**, never logged or persisted in plaintext |
| Activation | Preconditions (domain-model §1.6) with a list of missing items; exclusive row lock; compare-and-set `Setup → Active` |
| Status changes | Exclusive row lock on store (or organization); compare-and-set; audit event with before/after status |

**Storefront (Angular SSR)**

| Deliverable | Detail |
|---|---|
| Locale routing | First path segment is the locale (RA-3), including single-locale stores. `/` → default locale without `Accept-Language`; unsupported or missing locale → same path in the default locale (M1-D3). |
| Status pages | `Setup` → "Coming soon" (HTTP 200, `noindex` meta tag and `X-Robots-Tag`). `Suspended` → branded **503** with `Retry-After`, `no-store`, display name, support contacts (M1-D4). Unknown host → platform 404, `no-store`, no store data. Resolution failure (database down) → generic platform 503, `no-store`. |
| Shell | Header with resolved display name (with `lang`/`dir` of its source locale), footer with support contacts and brand name; `<html lang dir>` per active locale; logical CSS properties only |
| SEO basics | Canonical URL on the primary host; `hreflang` alternates for supported locales plus `x-default`; none for single-locale stores |
| Cookie and query rules | SSR responses never set cookies and never forward browser cookies. **Public responses do not depend on query parameters** (the only M1 requirement derived from RA-7; the allow-list itself is configured with the shared cache, from M4). |

**Tests (gates)**

| Gate | Content |
|---|---|
| **Tenant-isolation harness** | Two-store fixture builder (`brand-a`, `brand-b`, different organizations, different locales); helper asserting a store-host request returns only its own store's data; applied to every storefront endpoint from now on |
| **Host-spoofing suite** | TH-1, TH-4, TH-6, TH-7, TH-10 (ADR-04 §4d) re-run **with real store resolution**: forged headers from the browser and on SSR-originated calls never change the resolved store |
| **Schema isolation test** | Enumerates tables; fails if a store-scoped table lacks `store_id NOT NULL` or a same-store reference lacks a composite foreign key (ADR-02) |
| Invariants touched | INV-1, INV-9 (store side), INV-12 (status read from the database), INV-13 (display name in default locale) |

### Out of scope (M1)
Staff login and admin API (M2); `disable-staff-user`, `enable-staff-user`, `reset-staff-password` commands (M2 — they need sessions; reset revokes sessions per A5); settings editing (M2); logo and theme (Branding); shared cache, RA-7 allow-list, and deployment (ADR-12); "Track an order" / "My orders" pages (M8a / M9).

### Decisions inside M1 (need your approval)

| # | Question | Recommendation |
|---|---|---|
| M1-D1 | W1 step 3 creates **StoreBranding** with platform defaults, but Branding is designed in Phase 5. | Create a minimal `store_branding` row (store id only, meaning "platform defaults"); fields added in the Branding milestone. Keeps provisioning identical to W1. |
| M1-D2 | W1 step 3 creates the **OrderNumberSequence** (next value 1001), an Ordering table. | Create the table and row in M1 — tiny, and avoids a backfill later. |
| M1-D3 | Redirect status for `/` → `/ar/`. A permanent redirect (301) is cached by browsers indefinitely, but the default locale can change. | **302**, cacheable at the edge for ≤ 60 s (`public, max-age=0, s-maxage=60`). |
| M1-D4 | The suspended page must link to "Track an order" and "My orders" (domain-model §1.6), which do not exist until M8a/M9. | Omit the links in M1; add them in the milestones that build those pages (acceptance criteria there). |
| M1-D5 | Handle reserved-word list is open-ended ("`admin`, `api`, `www`, `mail`, `static`, `app`, …"). | Fix the list in M1 as configuration, adding at least `media` (ADR-09), `assets`, `cdn`, `status`, `help`, `support`, `docs`, `dev`, `staging`, `test`. |

### Acceptance criteria — M1

| # | Criterion | How verified |
|---|---|---|
| M1-1 | `brand-a.localhost` and `brand-b.localhost` resolve to their own stores; each config response contains only that store's data. | Isolation harness (integration + Playwright) |
| M1-2 | An unknown host returns the platform 404 with `Cache-Control: no-store` and no store data; no fallback store ever. | Integration + Playwright |
| M1-3 | Host normalization: `Brand-A.localhost:443` and `brand-a.localhost.` resolve to `brand-a` (TH-10). | Unit + integration |
| M1-4 | **Host spoofing:** forged `X-Forwarded-Host`, `Forwarded`, or `X-Platform-Host` from a browser (TH-1) or on an SSR-originated call (TH-4) never changes the resolved store; the API ignores its own `Host` header (TH-6); a request without a valid key never reaches resolution (TH-2/TH-3). | E2E through the edge + API integration |
| M1-5 | `/` redirects (302) to the default locale regardless of `Accept-Language`; `/fr/x` and an unsupported `/en/x` on an Arabic-only store redirect to `/ar/x`; single-locale stores still use the prefix. | Playwright |
| M1-6 | `<html lang dir>` is `ar`/`rtl` and `en`/`ltr` respectively; the display name falls back to the default locale when the requested locale has no value, and carries its source locale. | Playwright + unit |
| M1-7 | `Setup` → "Coming soon", HTTP 200, `noindex`, `public, max-age=0, s-maxage=60`. `Suspended` store **or** suspended organization → **503** with `Retry-After` and **`Cache-Control: no-store`**, plus support contacts. `Active` → shell. | Integration + Playwright, one test per status combination, asserting headers |
| M1-8 | After a status change, rendering reflects it within 60 s (resolution cache), while operator commands and any status check used for business decisions read the database immediately. | Integration test with a controllable clock (`TimeProvider`) |
| M1-9 | Provisioning is one transaction: injected failure at each step leaves **no rows**; re-running with the same handle or host fails without side effects. | Integration tests with failure injection |
| M1-10 | Provisioning with an existing `Active` staff email reuses the identity, changes no credential, prints no password, and creates exactly one new membership; a `Disabled` email is refused with nothing created. | Integration |
| M1-11 | The temporary password is generated (no option to supply one), appears on the console **only after commit**, and never appears in logs, audit events, or the database (hash only, vetted algorithm per ADR-05, `mustChangePassword = true`). | Integration test capturing logs and scanning tables |
| M1-12 | Activation with missing preconditions is refused with the list of missing items; repeat activation is refused ("already active"); the handle cannot change after activation. | Integration |
| M1-13 | Every operator command without an operator identifier is refused; every successful command writes exactly the audit events W1/W11 specify, with the identifier as `actor` and no secrets in `details`. | Integration |
| M1-14 | `GET /api/storefront/config` sets `public, max-age=0, s-maxage=60`, never sets a cookie, and does not include `organizationId`, `lowStockThreshold`, or `version`. | Contract test against the generated OpenAPI response schema + header assertions |
| M1-15 | Public responses are identical with and without arbitrary query parameters (e.g., `?utm_source=x`, `?page=2`). | Integration + Playwright |
| M1-16 | Schema isolation test passes and fails deliberately on a fixture table without `store_id`. | CI |
| M1-17 | Canonical and `hreflang` tags are correct for bilingual and single-locale stores and use the primary host. | Playwright |

### Definition of done (applies to M0, M1, and every later milestone)
- All acceptance criteria automated where marked, and green in CI.
- Every new store-scoped endpoint covered by the isolation harness; every new public endpoint covered by the host-spoofing suite.
- No secrets, personal data, or tokens in code, logs, or fixtures.
- Documentation updated in the same change when behavior or decisions change.
- No deferred feature implemented.

---

## 4. How M0 + M1 set up the later vertical slices

| Later milestone | What it inherits from M0 + M1 |
|---|---|
| M2 Staff auth | Staff tables and memberships already exist (created by provisioning); trusted-path middleware; resolver and error format; CI and integration harness for the authorization matrix |
| M3 Catalog and media | `storeId` resolution, composite-key pattern, schema isolation test, settings (locales, currency) |
| M4 Storefront catalog | Locale routing, shell, SEO tags, cache headers, SSR-without-cookies rule; RA-7 allow-list added with catalog query parameters |
| M5 Inventory | Real-PostgreSQL harness for the W8 idempotency and concurrency tests (IN-1–IN-9) |
| M6–M7 Cart and checkout | Effective status read from the database (INV-12) and the concurrency harness; order number sequence row exists |
| M8–M9 | Suspended-page links added; isolation harness extended to order and account endpoints |
| Hosting (ADR-12) | Edge rules already proven locally; only the production edge (verified against ADR-04 §4f) and shared cache are added |

Each later milestone then follows the same shape: migration → backend operation with tests (including isolation and invariants) → OpenAPI contract → generated types → UI → E2E test — one slice at a time.
