# Milestones M0 + M1 — Scope and Acceptance Criteria

| | |
|---|---|
| **Status** | **Scope approved** (2026-10-18; M1-D1–M1-D5 approved) · **gates G1–G3 met** (G3 verified 2026-10-19) — M0 proceeds step by step per §1a |
| **Last updated** | 2026-10-18 (review decisions recorded; ADR-04 revision 4) |
| **Depends on** | [architecture-decisions.md](architecture-decisions.md) — ADR-01–03, 05, 11, X approved; ADR-04 revision 4 approved 2026-10-18 — and requirements v1.9 (approved) |

> **Goal.** Leave the repository with a working, tested foundation on which every later milestone is built as an end-to-end vertical slice: one codebase, a real database, CI, a **proven trusted-host path**, and **correct, tested tenant resolution** — before any commerce data exists.
>
> Tools and versions named here are those of the approved ADRs. Trusted-host items follow ADR-04 **revision 4** (approved 2026-10-18; commit `849fe35`, merge `c9edca5`).
>
> **Not in M0/M1:** catalog, media, inventory, cart, checkout, orders, payments, staff login, customer accounts.

---

## 1. Gates and prerequisites (before M0 starts)

**Implementation gates — all three must be met before any application code is written:**

| # | Gate | Status |
|---|---|---|
| **G1** | **ADR-04 revision 4 reviewed and approved** (separate credentials, network binding, tests TH-1–TH-14) | ✅ Approved 2026-10-18 — revision 4 as merged to `main` (commit `849fe35`, merge `c9edca5`) |
| **G2** | Planning documents consistent (requirements, domain model, system behavior, ADRs, change record, roadmap, this document) | ✅ Complete — reconciled and merged to `main` via PR #3 (merge `c9edca5`) |
| **G3** | **Branch protection on `main`** configured by the repository owner: pull request required (**0 required approvals** — solo-developer project), no direct pushes, no force pushes, no deletion, no bypass. *(Required CI status checks are added in M0, once the workflow exists and its check names are stable — M0-17.)* | ✅ **Configured and verified 2026-10-19** (read-only GitHub API check): classic protection rule on `main` — pull request required, `required_approving_review_count: 0`, `enforce_admins` on (no bypass), force pushes and deletions disabled, no push or bypass allowances, no required status checks yet (added at M0-17). Direct-push rejection follows from this configuration; it was not exercised with a live push. |

**Prerequisites:**

| # | Prerequisite | Status |
|---|---|---|
| P1 | ADR-01, 02, 03, 11 and the cross-cutting conventions | ✅ Approved 2026-10-18 |
| P1a | ADR-04 (trusted-host flow) | ✅ Revision 4 approved 2026-10-18 (= G1) |
| P2 | ADR-05 **password hashing** part | ✅ Approved 2026-10-18 (library vetted in M1) |
| P3 | RA-1, RA-2, RA-3 | ✅ Approved 2026-10-18 |
| P4 | v1.9 items C1 (TTL-only caching) and A4 (generated temporary passwords only) | ✅ Approved 2026-10-18 |
| P5 | Decisions M1-D1 … M1-D5 and U-3 (`Retry-After` 3600 s) | ✅ Approved 2026-10-18 |
| P6 | Node/Angular pair chosen at M0 start from Angular's **official compatibility table** (not Node 26 merely because it is LTS — ADR-11) | ✅ Done 2026-10-19: Node 24.15.0 + Angular 22.2.3 (TypeScript 6.0.3), checked against Angular's compatibility table; pins in commit `a634721`, Angular pair recorded in ADR-11 with M0 step 4 |
| P7 | Phase 4 items needed before M1 ([system-behavior §7.4](system-behavior.md#74-items-deferred-to-the-architecture-phase)): **P4-1** suspension synchronization; **P4-2** recording refused/failed operator commands; **P4-4** confirmation of M1-D3's scope | P4-1 ✅ Approved 2026-10-18 · P4-2 ✅ Approved 2026-10-19 · P4-4 ✅ Approved 2026-10-19 |

**Not required before M0/M1:** ADR-06–10 (approved; needed from M2/M3), ADR-12 and **U-5** (open; required before the first shared environment, not for local M0/M1), RA-4–RA-8 (RA-4, RA-7, RA-8 approved with clarifications; RA-5 and RA-6 revised in the second review and approved 2026-10-18 — [change record §7](proposed-doc-changes.md#7-ra-1-to-ra-8--second-review-2026-10-18); RA-7 is first needed in M4, RA-8 in M5/M7). RA-1–RA-3 remain approved; their clarifications (declared operator identity and audit outcome, deterministic redirects) apply to the M1 operator commands and locale routing.

### 1a. M0 start checklist (execution order — begins only after G3 is verified)

Each step is one pull request into `main`, reviewed before the next starts. Architecture approval does not mean implementation is complete: every step is "done" only when its acceptance criteria pass in CI (or, for manual criteria, are recorded).

| Step | Work | Acceptance criteria covered | Done when |
|---|---|---|---|
| **1. Toolchain verification** | Check the official sources: .NET support policy (.NET 10 LTS), ASP.NET Core 10 (ships with the .NET 10 SDK), Angular's version-compatibility table (Angular major ↔ Node.js ↔ TypeScript), Node.js release schedule. Choose the Node/Angular pair from the Angular table (P6). Record versions and sources in the PR; add `global.json`, `.nvmrc`, `engines`, `.npmrc` (`save-exact=true`), `.gitignore`, `.gitattributes`. | P6 | Pair and sources recorded; pins committed |
| **2. .NET API skeleton + database baseline** | `.slnx`; `Directory.Build.props` (nullable, warnings as errors, analyzers, `RestorePackagesWithLockFile`); `Directory.Packages.props`; `Api` and `Cli` hosts; empty domain projects with the allowed reference graph; `Infrastructure` with EF Core 10 + Npgsql; initial migration; migration vs. application database roles; Aspire AppHost with PostgreSQL. | M0-11, M0-12 | Migrations apply from zero; architecture test fails on a forbidden reference |
| **3. Health checks + reproducible installs** | `GET /api/health`, `/api/health/ready`; options validation on start; Problem Details; request id; JSON logging with redaction; default `private, no-store`; committed `packages.lock.json`; `dotnet restore --locked-mode`. | M0-9, M0-10, M0-14 (backend), M0-15 | Tests for each criterion pass locally |
| **4. Angular workspace** | Angular CLI workspace under `/frontend`: `storefront` with SSR, `admin` client-only, design-system and i18n/formatting libraries, `<html lang dir>` wiring; UI strings via Angular built-in i18n, one build per locale (T-1, approved 2026-10-19); committed `package-lock.json`; `npm ci`. | M0-3, M0-14 (frontend) | Both apps build; SSR page renders |
| **5. OpenAPI contracts + drift check** | Build-time `contracts/openapi.json`; `openapi-typescript` 7.13.0 via `npm run contracts:generate` into the `api-contracts` library; one example endpoint typed end to end; drift-check script; shared test-vector location. | M0-13 | Changing an endpoint without regenerating fails the check |
| **6. CI + trusted-host boundary + safe logging** | GitHub Actions workflow (restore/build/test both stacks, migrations, drift check, bundle scan, secret scan, dependency audit, E2E); Caddy local edge with header deletion and one credential per route; SSR ingress-key check and server-only interceptor with the SSR-to-API key; API trusted-path middleware with credential + source-network classification and SSR allow-list; test-only diagnostics on API and SSR; three keys in local secrets, redacted in logs. Then the owner adds the stable CI check names as required status checks. | M0-2, M0-16, M0-17 | Workflow green on a PR; required checks configured (manual, recorded) |
| **7. Agreed M0 tests** | Complete and automate the remaining criteria: host-spoofing and credential tests TH-1–TH-14 (local), diagnostics absent in `Production`, configuration fail-fast for both servers, fresh-clone check. | M0-1, M0-4–M0-8d, remaining items | All M0-1–M0-17 met; M0 closed by review |

**Not in M0 (or M1):** catalog, media, inventory, cart, checkout, orders, payments, staff login, customer accounts.

---

## 2. M0 — Walking skeleton

**Goal:** prove the chosen stack end to end — including the trusted-host path — with no business features.

### Scope

| Area | Deliverables |
|---|---|
| Repository | Layout per ADR-03: `/frontend` (Angular CLI workspace — `storefront` with SSR, `admin` client-only; libraries for design-system foundation, i18n/formatting, and generated `api-contracts`), `/backend` (.NET solution), `/contracts`, `/dev` |
| Backend | .NET 10 solution (`.slnx`, central package management, nullable on, warnings as errors): `Api` host (ASP.NET Core Minimal APIs), `Cli` host (console app, no commands yet), empty domain projects with the **allowed project-reference graph** from system-behavior §1, `Infrastructure`. API provides: options validation on start (fail fast), JSON logging with compliance redaction, Problem Details error format with `code` (ADR X), request id, OpenTelemetry wiring, `GET /api/health` (liveness) and `GET /api/health/ready` (database reachable). Default response header `Cache-Control: private, no-store` unless an endpoint opts in to public caching. The `Worker` host is added in M2. |
| Database | PostgreSQL (local container); EF Core 10 + Npgsql; an initial migration; CI applies migrations from scratch; separate migration and application database roles |
| **Trusted-host path** (ADR-04 revision 4) | **Three separate credentials:** API edge key (edge → API), SSR ingress key (edge → SSR), SSR-to-API key (SSR → API). Local edge (Caddy) implementing edge rules 1–4: deletes all client host-override and `X-Platform-*` headers, sets `X-Platform-Host`, adds **exactly one credential per route** (never the API edge key on SSR routes). SSR server: verifies the SSR ingress key before rendering; server-only interceptor calls the API on `INTERNAL_API_BASE_URL` with the verified host and the SSR-to-API key, `GET` only, forwarding no cookies or incoming credentials; configuration has **no** API edge key setting. API: trusted-path middleware classifying callers by credential **and** source network (rejects the SSR ingress key outright); SSR caller limited to the allow-list (in M0: `GET /api/health*`); `X-Platform-Host` as the only host source; Forwarded Headers never processing `X-Forwarded-Host`. Test-only diagnostics on API and SSR (ADR-04 §4d). Store resolution itself comes in M1. |
| Contracts | `contracts/openapi.json` generated at build (`Microsoft.Extensions.ApiDescription.Server`); **`openapi-typescript` 7.13.0** (exact pin) via `npm run contracts:generate` = `openapi-typescript ../contracts/openapi.json --output projects/api-contracts/src/lib/schema.d.ts`; one example endpoint typed end to end; shared test-vector file location established |
| Frontend | Storefront SSR renders one page that calls `/api/health` server-side; admin builds and serves a placeholder page; `<html lang dir>` wiring present |
| Local environment | Aspire AppHost (ADR-11) starting PostgreSQL, the API, and the Angular apps; Caddy serving `https://*.localhost` and `https://admin.localhost` with trusted local certificates |
| **Reproducible installs** | `global.json` (.NET 10 SDK, `rollForward: latestPatch`); `RestorePackagesWithLockFile=true` + committed `packages.lock.json`; `.nvmrc` + `engines` with the Node version chosen under P6 (baseline: Node 24 LTS ≥ 24.13.1); `.npmrc` with `save-exact=true`; committed `package-lock.json`; Angular major chosen under P6 (baseline 22.x), Playwright 1.62.x, openapi-typescript 7.13.0 pinned exactly |
| Configuration hygiene | `dotnet user-secrets` / local secret files for the three trusted-path keys (each process receives only its own); `.env.example` / `appsettings.Development.json` with placeholders only; `.gitignore` covering secrets, `bin/`, `obj/`, `node_modules/`, build output; `.gitattributes` normalizing line endings |
| CI | GitHub Actions on every push and pull request: `dotnet restore --locked-mode` / build / test; migrations from scratch; **OpenAPI drift check** (`dotnet build` → `npm run contracts:generate` → `git diff --exit-code -- contracts/ frontend/projects/api-contracts/`); `npm ci`, lint, test, production builds; **browser-bundle scan** for all trusted-path key names/values and the internal API URL (TH-8); secret scan; dependency audit (`dotnet list package --vulnerable`, `npm audit`); E2E through the local edge |
| Test harness | xUnit v3 projects; `WebApplicationFactory` + Testcontainers for .NET (PostgreSQL) + Respawn helper; architecture test project (ArchUnitNET or NetArchTest) asserting the domain reference graph; Playwright (TypeScript) running through the local edge; Angular unit test runner configured |
| Documentation | `README.md`: prerequisites (.NET 10 SDK, Node.js per `.nvmrc`, container runtime, trusting the local certificate), one-command local start, how to run each test layer, and the branch-protection configuration (G3, M0-17) |

### Out of scope
Any business table, authentication, store resolution, styling beyond a placeholder, deployment.

### Acceptance criteria — M0

| # | Criterion | How verified |
|---|---|---|
| M0-1 | A fresh clone starts the full local stack with the documented commands. | Manual check on a fresh clone in a clean environment (new directory or clean machine/container), following README only — solo-developer project |
| M0-2 | `https://brand-a.localhost/` renders a server-side page whose initial HTML contains a value fetched from `/api/health` **through the SSR server's internal call**. | Playwright inspecting the initial HTML |
| M0-3 | `https://admin.localhost/` serves the admin placeholder; `/api/health` is reachable same-origin on both hosts. | Playwright |
| M0-4 | **TH-1:** a browser request with forged `X-Forwarded-Host`, `Forwarded`, and `X-Platform-Host` headers reaches the API with the **edge-set** `X-Platform-Host`. | E2E through the edge, asserting via the test-only diagnostic |
| M0-5 | **TH-2 / TH-3:** direct requests to the API or the SSR server that bypass the edge (no key, wrong key, forged host header) are rejected with `403`; the SSR server renders nothing and makes no API call. | Integration tests against the API and SSR ports |
| M0-6 | **TH-4 / TH-5:** the SSR server's internal API call carries the verified host and the SSR-to-API key; that key used on a `POST`, or on admin, auth, cart, checkout, account, or any non-allow-listed route, is rejected with `403`. | E2E + API integration tests |
| M0-7 | **TH-8:** browser bundles contain no trusted-path key names/values and no internal API URL. | CI bundle scan |
| M0-8 | **TH-9:** the API and SSR diagnostics return `404` in the `Production` configuration; startup fails if a diagnostic flag is set outside `Testing`. | Integration test starting each server in each configuration |
| M0-8a | **TH-11:** the SSR ingress key presented to the API on any route (public, admin, write) is rejected with `403`. | API integration test |
| M0-8b | **TH-12:** a valid API edge key presented from an SSR source address on privileged routes, and a valid SSR-to-API key from an edge or unknown address, are rejected with `403`. | API integration test setting the connection's remote address |
| M0-8c | **TH-13:** through the edge, a store-page request (including a client-forged API edge key) reaches the SSR server with only the edge-set SSR ingress key and **no** API edge key; an `/api/*` request reaches the API with only the API edge key. | E2E with API and SSR diagnostics |
| M0-8d | **TH-14:** the SSR server's configuration has no API edge key setting; startup fails if one is supplied. | Configuration test |
| M0-9 | The API and SSR server refuse to start with missing or invalid configuration (including missing keys) and name the missing setting without printing secret values. | Tests of configuration validation on start |
| M0-10 | Logs redact cookies, `Authorization`, all three trusted-path keys, and data classified as tokens, passwords, phones, emails. | Unit test on the logging/redaction configuration |
| M0-11 | Migrations run from zero on an empty database in CI; the application role cannot run DDL. | CI integration job |
| M0-12 | A project reference that violates the domain dependency graph (e.g., `Catalog` → `Ordering`) fails the build or the architecture test. | Architecture test with a deliberate violation fixture |
| M0-13 | Changing an endpoint without regenerating `openapi.json` and the TypeScript types fails CI; the Angular example call compiles only against the generated types. | CI drift check |
| M0-14 | Installs are reproducible: CI uses `dotnet restore --locked-mode` and `npm ci`; a dependency change without an updated lockfile fails CI; tool versions match the baseline files. | CI |
| M0-15 | An endpoint that does not opt in to public caching returns `Cache-Control: private, no-store`. | Integration test |
| M0-16 | No secrets in the repository; placeholders only in example configuration. | CI secret scan + review |
| M0-17 | Once the CI workflow exists, the repository owner adds its checks as **required status checks** to the `main` branch protection configured under G3. | **Manual** — owner reviews the settings and records the date in the README |

**Manual repository configuration (not provable by CI):** branch protection is configured in two steps — the base rules **before M0** (G3) and the required CI checks **during M0** (M0-17). CI can only show that checks run and pass; it cannot prove that GitHub enforces them. *(Requires the owner's GitHub access.)*

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
| Host normalization | Lowercase, strip port, strip trailing dot, validate syntax — from `X-Platform-Host` only, accepted only from a caller classified as *edge* or *SSR* by credential and source network (ADR-04 revision 4) |
| Store resolver | Host → StoreDomain → Store; **unknown host → `STORE_NOT_FOUND`, never a fallback**; in-process cache ≤ 60 s used for rendering decisions only (C1) |
| Effective status | Organization `Suspended` ⇒ store behaves as `Suspended`. Operator commands read the **current** status from the database, never the cache. |
| `GET /api/storefront/config` | Storefront view per domain-model §1.9: brand name, resolved display name + source locale, effective status, supported/default locales, currency, `pricesIncludeTax`, timezone, shipping rule, support contacts, platform limits. **Excludes** `organizationId`, `lowStockThreshold`, `version`. Added to the SSR-key allow-list. |
| Cache headers | Config and coming-soon: `public, max-age=0, s-maxage=60`, no `Set-Cookie`. Suspended `503`: `no-store` + `Retry-After` (U-3: 3600 *[DEFAULT]*). Errors (404, 5xx): `no-store`. Everything else: `private, no-store` (M0 default). |
| Operator CLI commands (`Cli` host) | `provision`, `activate`, `suspend-store`, `reactivate-store`, `suspend-organization`, `reactivate-organization`. Each requires an **operator identifier** (RA-1 — recorded as a self-declared audit label, not verified identity) and writes PlatformAuditEvents in the same transaction on success; refusals and failures per P4-2 (system-behavior W1). Suspension commands require a reason. |
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
| **Host-spoofing suite** | TH-1, TH-4, TH-6, TH-7, TH-10–TH-13 (ADR-04 §4e) re-run **with real store resolution**: forged headers from the browser and on SSR-originated calls never change the resolved store, and no SSR-held credential reaches a non-public route |
| **Suspension-race harness** (P4-1) | Real-PostgreSQL harness for SU-1, SU-2, SU-6, and SU-7 against the M1 status-change commands, using a test-only fixture operation that follows P4-1 rules 2–4; later milestones add their categories (SU-3) |
| **Schema isolation test** | Enumerates tables; fails if a store-scoped table lacks `store_id NOT NULL` or a same-store reference lacks a composite foreign key (ADR-02) |
| **Operator audit outcomes** (P4-2) | AU-1–AU-6 (system-behavior W1, *Required tests*) against real PostgreSQL with the M1 operator commands, including failure injection for AU-4 and AU-5; built with the M1 CLI (no application code exists before M1) |
| Invariants touched | INV-1, INV-9 (store side), INV-12 (status read from the database), INV-13 (display name in default locale) |

### Out of scope (M1)
Staff login and admin API (M2); `disable-staff-user`, `enable-staff-user`, `reset-staff-password` commands (M2 — they need sessions; reset revokes sessions per A5); settings editing (M2); logo and theme (Branding); shared cache, RA-7 allow-list, and deployment (ADR-12); "Track an order" / "My orders" pages (M8a / M9).

### Decisions inside M1 (approved 2026-10-18 as proposed)

| # | Question | Recommendation |
|---|---|---|
| M1-D1 | W1 step 3 creates **StoreBranding** with platform defaults, but Branding is designed in Phase 5. | Create a minimal `store_branding` row (store id only, meaning "platform defaults"); fields added in the Branding milestone. Keeps provisioning identical to W1. |
| M1-D2 | W1 step 3 creates the **OrderNumberSequence** (next value 1001), an Ordering table. | Create the table and row in M1 — tiny, and avoids a backfill later. |
| M1-D3 | Redirect status for `/` → `/ar/`. A permanent redirect (301) is cached by browsers indefinitely, but the default locale can change. | **302**, cacheable at the edge for ≤ 60 s (`public, max-age=0, s-maxage=60`). *Confirmed 2026-10-19 (P4-4):* the same for unsupported and missing locale prefixes; `Location` is a relative path; M1 drops the query string (allow-listed parameters kept from M4); no `Set-Cookie`. |
| M1-D4 | The suspended page must link to "Track an order" and "My orders" (domain-model §1.6), which do not exist until M8a/M9. | Omit the links in M1; add them in the milestones that build those pages (acceptance criteria there). |
| M1-D5 | Handle reserved-word list is open-ended ("`admin`, `api`, `www`, `mail`, `static`, `app`, …"). | Fix the list in M1 as configuration, adding at least `media` (ADR-09), `assets`, `cdn`, `status`, `help`, `support`, `docs`, `dev`, `staging`, `test`. |

### Acceptance criteria — M1

| # | Criterion | How verified |
|---|---|---|
| M1-1 | `brand-a.localhost` and `brand-b.localhost` resolve to their own stores; each config response contains only that store's data. | Isolation harness (integration + Playwright) |
| M1-2 | An unknown host returns the platform 404 with `Cache-Control: no-store` and no store data; no fallback store ever. | Integration + Playwright |
| M1-3 | Host normalization: `Brand-A.localhost:443` and `brand-a.localhost.` resolve to `brand-a` (TH-10). | Unit + integration |
| M1-4 | **Host spoofing:** forged `X-Forwarded-Host`, `Forwarded`, or `X-Platform-Host` from a browser (TH-1) or on an SSR-originated call (TH-4) never changes the resolved store; the API ignores its own `Host` header (TH-6); a request without a valid credential over the expected network path never reaches resolution (TH-2, TH-3, TH-11, TH-12); the SSR server never receives the API edge key (TH-13). | E2E through the edge + API integration |
| M1-5 | `/` redirects (302) to the default locale regardless of `Accept-Language`; `/fr/x` and an unsupported `/en/x` on an Arabic-only store redirect to `/ar/x`; single-locale stores still use the prefix. Every locale redirect returns `Cache-Control: public, max-age=0, s-maxage=60`, a relative `Location` without the query string, and no `Set-Cookie` (LR-1, P4-4). | Playwright + integration |
| M1-6 | `<html lang dir>` is `ar`/`rtl` and `en`/`ltr` respectively; the display name falls back to the default locale when the requested locale has no value, and carries its source locale. | Playwright + unit |
| M1-7 | `Setup` → "Coming soon", HTTP 200, `noindex`, `public, max-age=0, s-maxage=60`. `Suspended` store **or** suspended organization → **503** with `Retry-After` and **`Cache-Control: no-store`**, plus support contacts. `Active` → shell. | Integration + Playwright, one test per status combination, asserting headers |
| M1-8 | After a status change, rendering reflects it within 60 s (resolution cache), while operator commands and any status check used for business decisions read the database immediately. | Integration test with a controllable clock (`TimeProvider`) |
| M1-9 | Provisioning is one transaction: injected failure at each step leaves **no rows**; re-running with the same handle or host fails without side effects. | Integration tests with failure injection |
| M1-10 | Provisioning with an existing `Active` staff email reuses the identity, changes no credential, prints no password, and creates exactly one new membership; a `Disabled` email is refused with nothing created. | Integration |
| M1-11 | The temporary password is generated (no option to supply one), appears on the console **only after commit**, and never appears in logs, audit events, or the database (hash only, vetted algorithm per ADR-05, `mustChangePassword = true`). | Integration test capturing logs and scanning tables |
| M1-12 | Activation with missing preconditions is refused with the list of missing items; repeat activation is refused ("already active"); the handle cannot change after activation. | Integration |
| M1-13 | Every operator command without an operator identifier is refused, writes no audit event, and logs a warning; every successful command writes exactly the audit events W1/W11 specify, with the identifier as `actor` and no secrets in `details`; every refused or failed command writes exactly one `Refused` / `Failed` event with a reason code in a separate transaction after rollback; a failure to persist a required audit record is logged, shown as a console warning, and returns a non-zero exit code (P4-2). | Integration — AU-1–AU-6 (system-behavior W1) |
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
