# White-Label E-commerce Platform — MVP Implementation Roadmap

| | |
|---|---|
| **Status** | **Updated after the review decisions of 2026-10-18.** Agreed so far: M0 + M1 first (no concurrency spike); order admin (M8) before customer accounts (M9). The roadmap as a whole is **pending your review**. |
| **Last updated** | 2026-10-18 |
| **Based on** | [requirements.md](requirements.md) v1.9 (approved), [domain-model.md](domain-model.md) (approved), [system-behavior.md](system-behavior.md), [architecture-decisions.md](architecture-decisions.md) (ADR-04 revision 4 pending review), [milestone-m0-m1.md](milestone-m0-m1.md) |

> **Principles.** Dependency-driven order; small end-to-end vertical slices (migration → backend operation with tests → OpenAPI contract → generated types → UI → E2E); tenant isolation, host spoofing, and domain invariants are **gates from the milestone that introduces them**, not final testing tasks. Deferred features (requirements §27) stay out.
>
> Milestone numbers (M0–M10) are independent of the documentation phases 1–5.

---

## 1. Implementation gates (before any application code)

| # | Gate | Status |
|---|---|---|
| G1 | **ADR-04 revision 4 reviewed and approved** (separate edge-to-SSR, edge-to-API, and SSR-to-API credentials; network binding; tests TH-1–TH-14) | ❌ Pending review |
| G2 | Planning documents consistent | ✅ Reconciled on branch `docs/record-review-decisions`; confirmed when merged |
| G3 | Branch protection on `main` (PR required, no direct/force pushes, no deletion) | ❌ Owner action |

Details: [milestone-m0-m1.md §1](milestone-m0-m1.md#1-gates-and-prerequisites-before-m0-starts).

---

## 2. Roadmap

| # | Milestone | Scope | Depends on | Exit gate (summary) |
|---|---|---|---|---|
| **M0** | Walking skeleton | .NET 10 solution (API, CLI), PostgreSQL + EF Core migrations, Angular storefront (SSR) + admin, OpenAPI → TypeScript contracts with drift check, **trusted-host path with three separate credentials**, CI, reproducible installs, safe logging and secrets | G1–G3 | M0-1–M0-17 ([milestone doc](milestone-m0-m1.md#acceptance-criteria--m0)) |
| **M1** | Tenancy core | Organization, Store, StoreDomain, StoreSettings, PlatformAuditEvent; staff identity tables created by provisioning; operator CLI (provision, activate, suspend/reactivate store and organization); trusted host resolution; locale routing and direction; Setup/Active/Suspended pages and cache headers | M0 | M1-1–M1-17; **isolation harness, host-spoofing suite, schema isolation test in CI** |
| **M2** | Staff auth and admin authorization | Staff sessions (custom handler), Argon2id/bcrypt, Antiforgery + `Origin` check, rate limiting, forced password change, `/admin/me`, membership → 404 / permission → 403 / store-status filter; **Worker** with outbox + Mailpit; staff password reset (revokes sessions), disable/enable commands; settings groups (no logo/theme); admin shell | M1 | Authorization matrix tests; outbox exactly-when-committed; staff reset revokes all sessions; operator alerts (U-2) |
| **M3** | Media and catalog admin | Media upload through the backend with NetVips (validation, auto-rotation, metadata stripping, renditions); categories; product save as one unit; inventory level + initial stock in the same transaction | M2 | INV-8 from both sides; product save all-or-nothing; cross-store media rejected |
| **M4** | Storefront catalog | Read layer: category tree, listing, product details, search; locale fallback; stock **states**; SSR SEO/OG/`hreflang`; ≤ 60 s public caching; **RA-7 query allow-list** for catalog parameters | M3 | No quantities exposed; cache keyed by host + locale; no cookies on cached responses |
| **M5** | Inventory admin | Receive/Remove (deltas), Count/Contain (expected `onHand`), **request ids for all four per `(storeId, requestId)`**, movements, filters, low-stock | M3 (parallel with M4 possible) | **IN-1–IN-9** against real PostgreSQL (INV-2, INV-10) |
| **M6** | Guest cart and quote | Guest cookie; add (request id), set, remove, adjust to available; quote with issues and fingerprint (no tax amount); expiry | M4, M5 | INV-6; replay never double-increments; foreign variant → identical 404 |
| **M7** | Guest checkout and placement | W5 end to end: idempotency key, shared/exclusive locks, conditional commit, order numbers, snapshots, Payment, events, access token, outbox confirmation email | M6 | **Concurrency suite** (last unit, double submit, lost response, suspension race); INV-2/3/4/7 |
| **M8a** | Order lifecycle (admin) | List/filters/details; transitions; staff cancel; mark as paid; delivery failed (terminal); record return; courier details; internal notes (incl. failed attempts, U-1); guest lookup and secret-link cancel (shared suspension lock) | M7 | Every W6 row = one test across five effect columns; D8 operations work while suspended |
| **M8b** | Edits and corrections | Item edit preview/apply (version + fingerprint, shared suspension lock); delivery-detail edits; payment correction with password re-entry | M8a | Stale version/fingerprint → 409, nothing written; INV-5; edit cannot commit after suspension |
| **M9** | Customer accounts | Register/login/logout; store-bound sessions; cart merge; account orders and cancel; password change and reset; logged-in checkout prefill | M8a (see §4) | Merge idempotency; all sessions revoked on reset; no enumeration |
| **M10** | Dashboard, jobs, hardening | Metrics (§20); cleanup jobs; reconciliation job and alerts; accessibility, Core Web Vitals, security review | M8, M9 | Reconciliation clean on seeded data; media cleanup never deletes order-referenced assets |
| **B** | Branding (Phase 5) | Logo, theme tokens, store-branded emails and 503 page | Phase 5 design; M3 | OQ1 resolved; platform defaults until then |

**Milestone-independent gates**

| Gate | When | Content |
|---|---|---|
| **First shared environment** | Before any deployment beyond local development | ADR-12 (hosting) decided; **U-5 resolved** (production edge supports per-route secrets, private origins, and source binding — or mutual TLS); production email provider (ADR-08); TH-12 source binding re-verified in that environment |
| **Commercial launch** | Before selling to real customers | **OQ3 / U-6 tax and invoicing review** (launch gate; no tax calculation added to the MVP); **OQ2** data-protection compliance |

---

## 3. Work split per milestone

| # | Backend | Frontend | Database | Tests (gates) |
|---|---|---|---|---|
| M0 | API/CLI hosts, trusted-path middleware, diagnostics (Testing only), health | SSR server key check + server-only interceptor; admin placeholder | Migration tooling, roles | TH-1–TH-14 (local), drift check, architecture test, lockfiles |
| M1 | Resolver, effective status, operator commands, audit | Locale routing, status pages, shell, SEO tags | Tenancy tables, composite keys | Isolation harness, host-spoofing suite with resolution, schema test |
| M2 | Auth module, authorization policies, outbox worker | Admin login, forced change, settings | Sessions, credentials, outbox | Authorization matrix, CSRF, rotation/revocation |
| M3–M5 | Media pipeline, product unit, inventory operations | Catalog/inventory admin, storefront pages | Catalog, media, inventory tables | INV-8, IN-1–IN-9, isolation per endpoint |
| M6–M7 | Quote engine, placement transaction | Cart, checkout, confirmation | Cart, order, payment tables | Concurrency suite on real PostgreSQL |
| M8–M10 | Order operations, accounts, jobs | Order admin, account area, dashboard | Indexes | W6 five-column tests, reconciliation, cleanup safety |

---

## 4. Dependencies and limited parallel work

1. **Store resolution before everything:** every store-scoped query needs a resolved `storeId`; the isolation harness exists before the first store-scoped table (M1).
2. **Auth before admin writes:** catalog, inventory, and order operations are permission-gated; CSRF, `Origin`, rate limiting, and the outbox land in M2. The auth module is **shared**: M9 reuses M2's hashing, sessions, CSRF, and rate limiting and adds only customer-specific flows — no duplicated work.
3. **Media → Catalog → Inventory:** product images need media ids; inventory levels are created in the product-save transaction (M3).
4. **Catalog + Inventory → Cart → Checkout:** the quote reads prices and availability; placement recomputes it in its transaction.
5. **Orders before order admin and account history.**
6. **M8 before M9 (agreed), with limited parallel work:**
   - Can run alongside M8 if capacity allows: registration, login/logout, cart merge, password change and reset (need only M2 + M6).
   - Must follow M8a: account order list, customer cancellation, logged-in checkout prefill (reuse M8a's views and cancel operation).
   - With one developer, run sequentially.
7. **Not postponed:** placement transaction and locks (M7), request-id idempotency (M5–M6), compare-and-set/versions (M8), session rotation/revocation (M2), outbox (M2), trusted-host credentials (M0).

---

## 5. Open items affecting the roadmap

| Item | Affects | Status |
|---|---|---|
| ADR-04 revision 4 | M0 start (G1) | Pending review |
| Branch protection | M0 start (G3) | Owner action |
| U-5 production edge capability | First shared environment | Open |
| ADR-12 hosting, production email provider | First shared environment | Deferred |
| OQ3 / U-6 tax and invoicing; OQ2 data protection | Commercial launch | Launch gates |
| OQ1 theme customization | Branding milestone | Phase 5 |
