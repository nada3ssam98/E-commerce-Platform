# Milestones M0 + M1 — Scope and Acceptance Criteria

| | |
|---|---|
| **Status** | **Draft — not approved; implementation not started** |
| **Last updated** | 2026-10-18 |
| **Depends on** | Approval of [architecture-decisions.md](architecture-decisions.md) revision 2 (.NET backend: ADR-01–04, ADR-05 password hashing, ADR-11, X) and of the items in [proposed-doc-changes.md](proposed-doc-changes.md) that touch M1 (C1, RA-1, RA-2, RA-3, RA-7) |

> **Goal.** Leave the repository with a working, tested foundation on which every later milestone is built as an end-to-end vertical slice: one codebase, a real database, CI, and **correct, tested tenant resolution** — before any commerce data exists.
>
> Where this document names a tool, it refers to the **proposed** ADR. If an ADR changes during review, the deliverable stays and the tool changes.

---

## 1. Prerequisites (before M0 starts)

| # | Prerequisite | Why |
|---|---|---|
| P1 | ADR-01, 02, 03, 04, 11 and the cross-cutting conventions approved | M0 builds exactly these |
| P2 | ADR-05 **password hashing** part approved | M1 provisioning stores a temporary password hash |
| P3 | RA-1, RA-2, RA-3 decided | They shape operator commands (RA-1, RA-2) and storefront routing (RA-3) |
| P4 | C1 (TTL-only caching) applied to the domain model | M1 implements store-resolution caching |
| P5 | Decisions M1-D1 … M1-D5 below | Small scope choices inside M1 |

Not required before M0/M1: ADR-06–10, ADR-12, the tax change (D2), C2–C4, A1–A5.

---

## 2. M0 — Walking skeleton

**Goal:** prove the chosen stack end to end with no business features.

### Scope

| Area | Deliverables |
|---|---|
| Repository | Layout per ADR-03: `/frontend` (Angular CLI workspace — `storefront` with SSR, `admin` client-only; libraries for design-system foundation, i18n/formatting, and generated `api-contracts`), `/backend` (.NET solution), `/contracts`, `/dev` |
| Backend | .NET 10 solution (`.slnx`, central package management, nullable on, warnings as errors): `Api` host (ASP.NET Core Minimal APIs), `Cli` host (console app, no commands yet), empty domain projects with the **allowed project-reference graph** from system-behavior §1, `Infrastructure`. API provides: options validation on start (fail fast), JSON logging with compliance redaction, Problem Details error format with `code` (ADR X), request id, OpenTelemetry wiring, `GET /api/health` (liveness) and `GET /api/health/ready` (database reachable). The `Worker` host is added in M2. |
| Database | PostgreSQL (local container); EF Core 10 + Npgsql; an initial migration; CI applies migrations from scratch; separate migration and application database roles |
| Contracts | `openapi.json` generated from the API at build and committed under `/contracts`; TypeScript types generated into the Angular `api-contracts` library; one example endpoint typed end to end; **shared test-vector file** location established (ADR-03) |
| Frontend | Storefront SSR renders one page that calls `/api/health` server-side; admin builds and serves a placeholder page; `<html lang dir>` wiring present |
| Local environment | Aspire AppHost (or Docker Compose, per ADR-11) starting PostgreSQL, the API, and the Angular apps; local edge proxy (Caddy, ADR-04) serving `https://*.localhost` and `https://admin.localhost`, routing `/api` to the API and pages to SSR or the admin bundle, **stripping client forwarded-host headers and setting the trusted host header** |
| Configuration hygiene | `dotnet user-secrets` for local backend secrets; `.env.example` / `appsettings.Development.json` with placeholders only; `.gitignore` covering secrets, `bin/`, `obj/`, `node_modules/`, build output; `.gitattributes` normalizing line endings |
| CI | GitHub Actions on every push and pull request: `dotnet restore` (locked mode) / build / test; migrations from scratch; **OpenAPI drift check** (regenerate, fail on diff); Angular install (frozen lockfile), lint, test, production builds; dependency audit (`dotnet list package --vulnerable`, `npm audit`) |
| Test harness | xUnit v3 projects; `WebApplicationFactory` + Testcontainers for .NET (PostgreSQL) + Respawn helper; architecture test project (ArchUnitNET or NetArchTest) asserting the domain reference graph; Playwright (TypeScript) configured to run through the local edge; Angular unit test runner configured |
| Documentation | `README.md`: prerequisites (.NET 10 SDK, Node.js LTS for Angular, container runtime, trusting the local certificate), one-command local start, how to run each test layer |

### Out of scope
Any business table, authentication, store resolution, styling beyond a placeholder, deployment.

### Acceptance criteria — M0

| # | Criterion | How verified |
|---|---|---|
| M0-1 | A fresh clone starts the full local stack with the documented commands. | Manual check by a second person following README only |
| M0-2 | `https://brand-a.localhost/` renders a server-side page whose HTML contains a value fetched from `/api/health` during SSR. | Playwright test inspecting the initial HTML (not client-rendered) |
| M0-3 | `https://admin.localhost/` serves the admin placeholder; `/api/health` is reachable same-origin on both hosts. | Playwright |
| M0-4 | A client-supplied `X-Forwarded-Host` (or the trusted header name) never reaches the API unchanged. | Integration test through the local edge: API echo in test mode shows the edge-set value |
| M0-5 | The API refuses to start with missing or invalid configuration and names the missing key without printing secret values. | Test of options validation on start |
| M0-6 | Logs redact cookies, `Authorization`, and data classified as tokens, passwords, phones, emails. | Unit test on the logging/redaction configuration |
| M0-7 | Migrations run from zero on an empty database in CI; the application role cannot run DDL. | CI integration job |
| M0-7a | A project reference that violates the domain dependency graph (e.g., `Catalog` → `Ordering`) fails the build or the architecture test. | Architecture test with a deliberate violation fixture |
| M0-7b | Changing an endpoint without regenerating `openapi.json` fails CI; the Angular example call compiles only against the generated types. | CI drift check |
| M0-8 | CI is green and **required** for merging into `main`. | Repository branch protection (requires your action on GitHub) |
| M0-9 | No secrets in the repository; `.env.example` contains placeholders only. | CI secret scan + review |

---

## 3. M1 — Tenancy core

**Goal:** every request to a store host is resolved to exactly one store (or to none), with the effective status applied, before any commerce feature exists — and the isolation test harness is in place.

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
| Host normalization | Lowercase, strip port, strip trailing dot; from the trusted header only (ADR-04) |
| Store resolver | Host → StoreDomain → Store; **unknown host → `STORE_NOT_FOUND`, never a fallback**; in-process cache ≤ 60 s (C1) |
| Effective status | Organization `Suspended` ⇒ store behaves as `Suspended` |
| `GET /api/storefront/config` | Storefront view per domain-model §1.9: brand name, resolved display name + source locale, effective status, supported/default locales, currency, `pricesIncludeTax`, timezone, shipping rule, support contacts, platform limits. **Excludes** `organizationId`, `lowStockThreshold`, `version`. |
| Cache headers | Config: `public, max-age=0, s-maxage=60`, no `Set-Cookie`. Errors (404, 5xx): `no-store`. |
| Operator CLI commands (`Cli` host) | `provision`, `activate`, `suspend-store`, `reactivate-store`, `suspend-organization`, `reactivate-organization`. Each requires an **operator identifier** (RA-1) and writes PlatformAuditEvents in the same transaction. Suspension commands require a reason. |
| Password hashing | Argon2id library **vetted** per ADR-05 (maintenance, official test vectors, PHC encoding, timing); bcrypt (cost ≥ 12) if none passes. Outcome recorded in the ADR. |
| Provisioning | Exactly W1: validation; staff lookup (new / reuse `Active` / refuse `Disabled` per RA-2); one transaction; temporary password hashed (per the vetted algorithm) and printed **once after commit**, never logged or persisted (or supplied via a secret, if A4 is approved) |
| Activation | Preconditions (domain-model §1.6) with a list of missing items; exclusive row lock; compare-and-set `Setup → Active` |
| Status changes | Exclusive row lock on store (or organization); compare-and-set; audit event with before/after status |

**Storefront (Angular SSR)**

| Deliverable | Detail |
|---|---|
| Locale routing | First path segment is the locale (RA-3). `/` → default locale; unsupported or missing locale → same path in the default locale (M1-D3). |
| Status pages | `Setup` → "Coming soon", `noindex` (meta tag and `X-Robots-Tag`). `Suspended` → branded **503** with `Retry-After`, display name, support contacts (M1-D4). Unknown host → platform 404, `no-store`, no store data. Resolution failure (database down) → generic platform 503, `no-store`. |
| Shell | Header with resolved display name (with `lang`/`dir` of its source locale), footer with support contacts and brand name; `<html lang dir>` per active locale; logical CSS properties only |
| SEO basics | Canonical URL on the primary host; `hreflang` alternates for supported locales plus `x-default`; none for single-locale stores |
| Cookie rule | SSR responses never set cookies and never forward browser cookies |

**Tests (gates)**

| Gate | Content |
|---|---|
| **Tenant-isolation harness** | Two-store fixture builder (`brand-a`, `brand-b`, different organizations, different locales); helper asserting a store-host request returns only its own store's data; applied to every storefront endpoint from now on |
| **Schema isolation test** | Enumerates tables; fails if a store-scoped table lacks `store_id NOT NULL` or a same-store reference lacks a composite foreign key (ADR-02) |
| Invariants touched | INV-1, INV-9 (store side), INV-12 (status checks — placement-side in M7), INV-13 (display name in default locale) |

### Out of scope (M1)
Staff login and admin API (M2); `disable-staff-user`, `enable-staff-user`, `reset-staff-password` commands (M2 — they need sessions); settings editing (M2); logo and theme (Branding); shared cache and deployment (ADR-12); "Track an order" / "My orders" pages (M8a / M9).

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
| M1-3 | Host normalization: `Brand-A.localhost:443` and `brand-a.localhost.` resolve to `brand-a`. A client-supplied forwarded host cannot change the resolved store. | Unit + integration through the edge |
| M1-4 | `/` redirects (302) to the default locale regardless of `Accept-Language`; `/fr/x` and an unsupported `/en/x` on an Arabic-only store redirect to `/ar/x`; single-locale stores still use the prefix. | Playwright |
| M1-5 | `<html lang dir>` is `ar`/`rtl` and `en`/`ltr` respectively; the display name falls back to the default locale when the requested locale has no value, and carries its source locale. | Playwright + unit |
| M1-6 | `Setup` → "Coming soon" with `noindex`. `Suspended` store **or** suspended organization → 503 with `Retry-After` and support contacts. `Active` → shell. | Integration + Playwright, one test per status combination |
| M1-7 | Status changes are visible on public pages within 60 s and in the backend's transactional checks immediately (verified by a status read that bypasses the resolution cache). | Integration test with a controllable clock |
| M1-8 | Provisioning is one transaction: injected failure at each step leaves **no rows**; re-running with the same handle or host fails without side effects. | Integration tests with failure injection |
| M1-9 | Provisioning with an existing `Active` staff email reuses the identity, changes no credential, prints no password, and creates exactly one new membership; a `Disabled` email is refused with nothing created. | Integration |
| M1-10 | The temporary password appears on the console **only after commit**, never in logs, audit events, or the database (hash only, vetted algorithm per ADR-05, `mustChangePassword = true`). | Integration test capturing logs and scanning tables |
| M1-11 | Activation with missing preconditions is refused with the list of missing items; repeat activation is refused ("already active"); the handle cannot change after activation. | Integration |
| M1-12 | Every operator command without an operator identifier is refused; every successful command writes exactly the audit events W1/W11 specify, with no secrets in `details`. | Integration |
| M1-13 | `GET /api/storefront/config` sets `public, max-age=0, s-maxage=60`, never sets a cookie, and does not include `organizationId`, `lowStockThreshold`, or `version`. | Contract test against the generated OpenAPI response schema + header assertions |
| M1-14 | Schema isolation test passes and fails deliberately on a fixture table without `store_id`. | CI |
| M1-15 | Canonical and `hreflang` tags are correct for bilingual and single-locale stores and use the primary host. | Playwright |

### Definition of done (applies to M0, M1, and every later milestone)
- All acceptance criteria automated where marked, and green in CI.
- Every new store-scoped endpoint covered by the isolation harness.
- No secrets, personal data, or tokens in code, logs, or fixtures.
- Documentation updated in the same change when behavior or decisions change.
- No deferred feature implemented.

---

## 4. How M0 + M1 set up the later vertical slices

| Later milestone | What it inherits from M0 + M1 |
|---|---|
| M2 Staff auth | Staff tables and memberships already exist (created by provisioning); resolver and error format; CI and integration harness for the authorization matrix |
| M3 Catalog and media | `storeId` resolution, composite-key pattern, schema isolation test, settings (locales, currency) |
| M4 Storefront catalog | Locale routing, shell, SEO tags, cache headers, SSR-without-cookies rule |
| M6–M7 Cart and checkout | Effective status (INV-12) and real-PostgreSQL concurrency harness; order number sequence row exists |
| M8–M9 | Suspended-page links added; isolation harness extended to order and account endpoints |
| Hosting (ADR-12) | Edge rules already proven locally; only the production edge and shared cache are added |

Each later milestone then follows the same shape: migration → backend operation with tests (including isolation and invariants) → contract schema → UI → E2E test — one slice at a time.
