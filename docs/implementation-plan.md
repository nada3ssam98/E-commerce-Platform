# White-Label E-commerce Platform — Implementation Plan (Phase 4 completion)

| | |
|---|---|
| **Status** | **Draft — pending your review.** *(2026-10-19: P4-3, P4-4, P4-6, P4-7 (b)(c), T-1, T-2 and the roadmap approved — recorded in PR #11; P4-5 held.)* Completes the Phase 4 planning: repository and project structure, module boundaries, enforcement rules, the remaining Phase 4 decisions, and the milestone plan. **Nothing here changes an approved decision.** |
| **Last updated** | 2026-10-19 |
| **Phase** | 4 — Architecture (completion) |
| **Based on** | `main` at `2e9ad78`: [requirements.md](requirements.md) v1.9, [domain-model.md](domain-model.md), [system-behavior.md](system-behavior.md), [architecture-decisions.md](architecture-decisions.md), [roadmap.md](roadmap.md), [milestone-m0-m1.md](milestone-m0-m1.md), [ui-design.md](ui-design.md) (draft) |

> **How to read this document.** Every significant statement carries one label:
> - **[APPROVED]** — already approved; the source is linked. Restated here only to place it in the structure.
> - **[TECH]** — a technical recommendation that follows from approved decisions and changes no product behavior. It applies unless you object.
> - **[PROPOSED]** — needs your explicit approval before it is recorded as decided (repository rule: Phase 4 items are decided only by explicit approval).
>
> Where this document and an approved document disagree, the approved document wins and this document is corrected.

---

## Table of contents

1. [Repository state (verified)](#1-repository-state-verified)
2. [Gap analysis](#2-gap-analysis)
3. [Phase 4 decision checklist](#3-phase-4-decision-checklist)
4. [Target repository structure](#4-target-repository-structure)
5. [Backend modules and dependencies](#5-backend-modules-and-dependencies)
6. [Persistence architecture](#6-persistence-architecture)
7. [Security, tenancy, and persistence rules](#7-security-tenancy-and-persistence-rules)
8. [API conventions](#8-api-conventions)
9. [Frontend structure](#9-frontend-structure)
10. [Test strategy](#10-test-strategy)
11. [Transaction and locking matrix (P4-7 (d))](#11-transaction-and-locking-matrix-p4-7-d)
12. [Milestone plan](#12-milestone-plan)
13. [Next actions](#13-next-actions)

---

## 1. Repository state (verified)

Checked on 2026-10-19 with `git fetch` and the GitHub API.

| Item | State |
|---|---|
| `main` | `2e9ad78` (merge of PR #7). Contains `docs/` (8 documents), `global.json` (SDK 10.0.402), `.nvmrc` (Node 24.15.0), `package.json` (engines only, no dependencies), `.npmrc`, `.gitignore`, `.gitattributes`. **No README, no source code, no tests, no CI workflow.** |
| Branch protection (G3) | **Not configured** (`main` returns "Branch not protected"). |
| PR #8 — P4-1 suspension synchronization | **Open**, no review, no checks. Owner approval (2026-10-18) recorded in the PR; **not on `main`**. |
| PR #9 — P4-2 audit of refused/failed operator commands | **Open**, no review, no checks. Owner approval (2026-10-19) recorded in the PR; **not on `main`**. Textual overlap with PR #8 (system-behavior header and §7.4, requirements decision log). |
| M0 step 1 (toolchain pins) | Merged as PR #6. The Node/Angular pair is recorded only in commit `a634721`'s message (Node 24.15.0 for Angular 22); the PR body is empty, and the exact Angular version and the compatibility-table source are not recorded. Merged **before G3** was met, which the M0 checklist requires first. |

---

## 2. Gap analysis

### 2.1 Contradictions between approved decisions

| # | Finding | Resolution |
|---|---|---|
| X-1 | **Provisioning crosses the dependency direction.** W1 step 3 creates the `OrderNumberSequence` row (an Ordering entity — domain-model §7.2; M1-D2) inside the provisioning transaction, but Store "depends on nothing" (system-behavior §1 rule 1), so Store code cannot create an Ordering row. | **[TECH]** The `provision` command handler in the **Cli host** owns the W1 transaction and calls Store (organization, store, domain, settings, branding row, staff identity, membership, audit) and Ordering (sequence row). One transaction, as W1 requires; domain ownership unchanged (§5.3). |

No other contradiction between approved decisions was found. ADR-02's placement-only lock wording is incomplete rather than contradictory; PR #8 aligns it.

### 2.2 Blockers — must be resolved before the stated step

| # | Gap | Blocks | Resolution |
|---|---|---|---|
| B-1 | G3 branch protection not configured | M0 step 2 | Owner action (milestone-m0-m1 §1) |
| B-2 | P4-1 and P4-2 approved but not on `main` | M1 | Owner reviews and merges PR #8 and PR #9 |
| B-3 | **How modules share persistence** is undefined: ADR-01 has one project per domain plus `Infrastructure`; P4-1's EF Core rule 1 (PR #8) requires **one `DbContext` per operation**, and placement spans Cart, Inventory, Ordering, and Store in one transaction | M0 step 2 (empty projects, initial migration) | **[TECH]** §6: one `PlatformDbContext` type in `Infrastructure`, model contributed by each module, migrations in a top-level `Persistence` project |
| B-4 | **Where shared auth, audit, outbox, email templates, reconciliation, and the suspension guard live** — not in the approved dependency graph | M0 step 2 (project graph) | **[TECH]** §5 |
| B-5 | **UI string translation mechanism** not decided (requirements §6 and ui-design §5 fix the behavior, not the mechanism) | M0 step 4 (workspace), M1 (shell, status pages) | **[APPROVED 2026-10-19]** (PR #11) T-1 (§3.2) |
| B-6 | P4-4 scope confirmation (unsupported/missing locale prefixes) | M1 locale routing | **[APPROVED 2026-10-19]** (PR #11) §3.1 |
| B-7 | Roadmap status "pending your review" | Plan of record after M1 | **[APPROVED 2026-10-19]** (PR #11) — change-controlled plan of record |

### 2.3 Gaps that are safe to resolve during implementation

| # | Gap | When | Resolution |
|---|---|---|---|
| D-1 | Angular state management "evaluated in Phase 4" (requirements §24) — not evaluated | M2 | **[APPROVED 2026-10-19]** (PR #11) T-2 |
| D-2 | API versioning not defined | M0 step 3 | **[TECH]** §8 — no version segment in the MVP |
| D-3 | ADR-11 "or" choices: ArchUnitNET vs NetArchTest; Shouldly vs plain asserts; CsCheck vs FsCheck | M0 step 2 | **[TECH]** ArchUnitNET, Shouldly, CsCheck |
| D-4 | Operator CLI argument parser not named | M1 | **[TECH]** `System.CommandLine` (stable 2.x; version verified when added) |
| D-5 | Table/column naming convention | M0 step 2 | **[TECH]** snake_case (matches milestone-m0-m1 table names), via explicit configuration or `EFCore.NamingConventions` |
| D-6 | Database roles beyond "migration vs application" | M0 step 2 | **[TECH]** §6.4 |
| D-7 | Secret-scan and dependency-update tooling | M0 step 6 | **[TECH]** gitleaks CLI in CI; Dependabot for NuGet, npm, GitHub Actions. *Done 2026-10-25: `Secret scan` CI job (gitleaks 8.30.1, `.gitleaks.toml`), `.github/dependabot.yml`* |
| D-8 | Problem Details `code` catalog not centralized | M0 step 3 | **[TECH]** one `ErrorCodes` registry in SharedKernel; codes listed in the OpenAPI document. *Done: registry in M0 step 3; `ProblemDetails` schema with the `code` enum generated from it in `contracts/openapi.json` (2026-10-25)* |
| D-9 | Documentation drift: ADR-11 Node baseline "≥ 24.13.1" vs. commit `a634721` "Angular 22 requires ^24.15.0"; the ADR-11 toolchain table is broken by a paragraph inside it; milestone P6 says "recorded in the M0 pull request" (the PR body is empty) | M0 step 4 | Verify against Angular's compatibility table when the workspace is created; fix ADR-11 and P6 in that PR. *Done: ADR-11 and P6 corrected 2026-10-19; the last stale "≥ 24.13.1" (milestone-m0-m1 §2) corrected 2026-10-25* |
| D-10 | Phase 5 UI design (ui-design.md) is a draft | Before M2 (admin shell) and M4 (storefront) | Owner review; M1 uses unbranded platform-default styles only |

---

## 3. Phase 4 decision checklist

### 3.1 Items P4-1 to P4-7 (system-behavior §7.4)

| # | Status and evidence | Question | Recommendation | Approval needed | Blocks M1 | Verified by |
|---|---|---|---|---|---|---|
| **P4-1** | Approved 2026-10-18 by the owner; PR #8 (open, not on `main`) | Synchronizing all prohibited writes with suspension | Shared org/store `FOR SHARE` locks, status read from the lock result, exclusive direct locks for row writers; EF Core rules 1–4 | Done — merge only | Yes (status-change side, EF rules) | SU-1–SU-8 |
| **P4-2** | Approved 2026-10-19 by the owner; PR #9 (open, not on `main`) | Recording refused/failed operator commands | Separate audit transaction after rollback; `targetId` optional; audit-write failure → error log, console warning, non-zero exit | Done — merge only | Yes | AU-1–AU-6 |
| **P4-3** | Approved 2026-10-19 (PR #11) | Does an idempotent replay signal that it was a replay? | **[APPROVED]** Replays return the normal response plus header `Idempotent-Replayed: true` (add-to-cart, placement, inventory adjustments). Body unchanged; clients may ignore it. Options rejected: no signal (harder to debug and test); a body field (changes contracts) | Yes | No (M5) | Replay tests assert the header on replay and its absence on first application |
| **P4-4** | Approved 2026-10-19 (PR #11) | Same status and headers for unsupported/missing prefixes? Query in `Location`? | **[APPROVED]** 302 with `public, max-age=0, s-maxage=60` for `/`, unsupported, and missing prefixes. `Location` is a relative path in the default locale. **M1 drops the query string** (consistent with M1-15); from M4 the RA-7 allow-listed parameters are kept | Yes | **Yes** | LR-1: each redirect case asserts status, `Cache-Control`, `Location`, and no `Set-Cookie` |
| **P4-5** | **Held** — conflict found on 2026-10-19 (see Recommendation) | Inventory fingerprint; is expected `onHand` sufficient for Count/Contain; error codes | **[PROPOSED]** Fingerprint = SHA-256 of canonical JSON `{v:1, operation, variantId, quantity or countedValue, expectedOnHand, reason, note (trimmed)}` plus the acting staff user id. Expected `onHand` **is sufficient**: a level `version` would also change on every order commit (`committed`) and reject counts on busy variants. Accepted limitation: an `onHand` change and exact reversal inside one count window is not detected, but it is visible in the movement log. Codes: `STOCK_CHANGED` (409, current values), `STOCK_SHORTAGE` (409), `REQUEST_ID_CONFLICT` (422), `VALIDATION_FAILED` (400). *Conflict: W8 approves `422` for business-rule rejections (limit exceeded; Contain with a counted value not below committed), and its fingerprint field list has no actor — awaiting the owner's answer* | Yes | No (M5) | IN-1–IN-9 plus a fingerprint-stability unit test |
| **P4-6** | Approved 2026-10-19 (PR #11) | Domain audit record for rejected inventory adjustments? | **[APPROVED]** No — a structured application log entry only (request id, variant, reason code, staff user id; no personal data). W8: rejections change nothing; INV-10 covers applied changes | Yes | No (M5) | A test asserts that a rejection writes no movement and emits one log entry |
| **P4-7** | (a) in PR #8; (b), (c) approved 2026-10-19 (PR #11); (d) open | Retry, timeouts, per-workflow matrix | **(b) [APPROVED]** No global `EnableRetryOnFailure`. One transaction runner re-runs the **whole** operation (locks, status check, writes) on `40P01` and `40001` only, at most 3 attempts with jittered backoff. Then `503 SERVICE_BUSY` with `Retry-After`; the client retries with the same request id or idempotency key. `55P03` (lock timeout) is not retried and maps to the same 503. **(c) [APPROVED]** For the application role: `lock_timeout = 5s`, `statement_timeout = 15s`, `idle_in_transaction_session_timeout = 30s`. Reconciliation runs as a separate read-only role with `statement_timeout = 120s`. **(d) [TECH]** matrix in §11, completed per milestone | Yes for (b), (c) | No (M2) | Injected `40P01` retried exactly to the limit; `23505` never retried; timeout settings asserted per role |

### 3.2 New items found in this review

| # | Question | Options | Recommendation | Approval needed | Needed by |
|---|---|---|---|---|---|
| **T-1** | How Angular translates UI strings | (a) Angular built-in i18n (`@angular/localize`, one build per locale, served under `/ar/` and `/en/`); (b) runtime translation library | **[APPROVED 2026-10-19] (a).** Admin locale-build selection is decided with the admin shell (M2, UQ6). No runtime dependency; fits the approved locale-prefixed URLs (each prefix serves its locale's build); smallest bundles; SSR-supported. Cost: switching locale reloads the page (it is already a navigation to another URL) and adding a locale means a rebuild — acceptable, since locales are platform-defined (`ar`, `en`). Store content (names, descriptions) is data in `LocalizedText`, not affected. | Yes — affects build and deployment shape | M0 step 4 |
| **T-2** | Angular state management | (a) signals + feature services, no store library; (b) NgRx | **[APPROVED 2026-10-19] (a).** Signals, `computed`, and Angular's resource APIs where stable in the pinned major; no NgRx or facade layer. The server is the source of truth; screens hold little client state. Reconsider only if a concrete cross-feature state problem appears. | Yes — requirements §24 deferred it to Phase 4 | M2 |

---

## 4. Target repository structure

**[APPROVED]** top-level layout per ADR-03 (`/frontend`, `/backend`, `/contracts`, `/dev`, `/docs`). Everything below the top level is **[TECH]** unless marked.

```text
/
├─ README.md                     prerequisites, one-command start, test commands, G3/M0-17 record (M0)
├─ global.json  .nvmrc  package.json  .npmrc  .gitignore  .gitattributes   toolchain pins (M0 step 1, done)
├─ .github/
│  ├─ workflows/ci.yml           build, test, drift, scans, E2E (M0 step 6)
│  └─ dependabot.yml             NuGet, npm, GitHub Actions updates
├─ backend/
│  ├─ Platform.slnx
│  ├─ Directory.Build.props      nullable, warnings as errors, analyzers, lock files
│  ├─ Directory.Packages.props   central package versions
│  ├─ src/
│  │  ├─ SharedKernel/           value types and primitives shared by all modules
│  │  ├─ Infrastructure/         PlatformDbContext, transaction runner, outbox writer, adapters
│  │  ├─ Modules/
│  │  │  ├─ Store/               Store & Tenant (incl. staff identity, audit, resolution)
│  │  │  ├─ Media/
│  │  │  ├─ Catalog/
│  │  │  ├─ Inventory/
│  │  │  ├─ Customers/           Customer & Authentication
│  │  │  ├─ Cart/
│  │  │  └─ Ordering/            Order, Payment & Fulfillment (+ reconciliation checks)
│  │  ├─ ReadModels/             cross-domain read views (storefront catalog, admin lists)
│  │  ├─ Persistence/            full EF model composition, migrations, design-time factory
│  │  ├─ Api/                    ASP.NET Core host: pipeline, auth handlers, endpoint registration
│  │  ├─ Worker/                 outbox dispatcher, Quartz jobs, email templates (M2)
│  │  ├─ Cli/                    operator commands (M1)
│  │  ├─ AppHost/                Aspire local orchestration (ADR-11)
│  │  └─ ServiceDefaults/        Aspire defaults: OpenTelemetry, health checks
│  └─ tests/
│     ├─ UnitTests/              pure domain rules, folder per module
│     ├─ IntegrationTests/       real PostgreSQL: API, CLI, constraints, locks, concurrency
│     ├─ ArchitectureTests/      dependency graph, endpoint conventions
│     └─ TestSupport/            fixtures: PostgreSQL container, Respawn, two-store builder, log capture, failure injection
├─ contracts/
│  ├─ openapi.json               generated at build, committed (ADR-03)
│  └─ test-vectors/              shared JSON vectors for rules duplicated in C# and TypeScript (ADR-03)
├─ frontend/                     Angular CLI workspace (§9)
│  ├─ angular.json  package.json  package-lock.json  tsconfig.json
│  ├─ projects/
│  │  ├─ storefront/             SSR application
│  │  ├─ admin/                  client-only application
│  │  ├─ design-system/          tokens, primitives, patterns
│  │  ├─ i18n/                   formatting: money, numbers, dates, digits, phone, direction
│  │  └─ api-contracts/          generated schema types + Problem Details helpers
│  └─ e2e/                       Playwright, runs through the local edge
├─ dev/
│  ├─ caddy/Caddyfile            local edge: header deletion, one credential per route (ADR-04)
│  └─ secrets.example.json       placeholder names only — real values in user-secrets / local env
├─ scripts/                      CI helpers: drift check, bundle scan (TH-8), docs link check
└─ docs/                         requirements, domain model, system behavior, ADRs, roadmap, milestones, UI design, this plan
```

### 4.1 Responsibilities and exclusions

| Location | Responsible for | Must **not** contain |
|---|---|---|
| `SharedKernel` | `Money`, `LocalizedText`, `Locale`, `Phone`, `Email`, `Hostname`, strongly typed ids (`StoreId`, …), `ErrorCodes`, result/error primitives | EF Core, ASP.NET Core, I/O, any domain rule |
| `Infrastructure` | `PlatformDbContext` (model supplied by modules — §6), transaction runner (P4-7 (b)), outbox **writer**, `IEmailSender` + MailKit adapter (ADR-08), `IMediaStorage` + S3 adapter (ADR-09), Data Protection key store (M2), security primitives (token generation and hashing, password-hasher wrapper, identifier HMAC — ADR-05/06), time (`TimeProvider`, Cairo conversion), logging redaction setup | Domain entities or rules; endpoints; references to any module |
| `Modules/*` | The domain's entities, EF configurations, invariants, operations, endpoint handlers (ADR-01: "handlers in the owning domain module"), permission names | Calls to modules above it; cross-domain display views (→ `ReadModels`); transaction begin/commit inside operations called by another module (§5.4) |
| `ReadModels` | Read-only queries combining domains (product page with stock **states**, category listings, admin order list) | Writes, business rules, state transitions (system-behavior §1 rule 2) |
| `Persistence` | Composing the full model (all modules), EF migrations, design-time factory, the migration bundle | Runtime business code |
| `Api` | Pipeline: trusted-path middleware, forwarded headers, store resolution filter, authentication handlers (`CustomerSession`, `StaffSession`), Antiforgery/`Origin` filters, rate limiting, Problem Details, endpoint registration from modules, OpenAPI | Business rules; SQL |
| `Worker` | Outbox dispatcher (`SKIP LOCKED`), Quartz jobs, email template rendering (Razor + `HtmlRenderer`) | Business operations (ADR-07: the worker never executes business operations) |
| `Cli` | Operator commands, orchestration of multi-module operator transactions (W1 — X-1), P4-2 audit handling | HTTP surface; business rules beyond orchestration |
| `AppHost` / `ServiceDefaults` | Local orchestration and shared host defaults | Production deployment configuration (ADR-12) |
| `tests/*` | See §10 | EF Core in-memory provider or SQLite for persistence tests (ADR-11) |
| `contracts/` | Generated OpenAPI document and shared vectors | Hand edits to `openapi.json` |
| `frontend/projects/api-contracts` | Generated `schema.d.ts` (never hand-edited) and hand-written Problem Details and type helpers | Business rules; runtime client generation |
| `dev/` | Local-only configuration with placeholders | Real secrets |

**Not created, deliberately:** per-module Application/Domain/Infrastructure layers (ADR-01 rejected Clean Architecture layers); a separate Notifications project (templates live in `Worker`, the only renderer); a separate Reconciliation project (checks live in `Ordering`, read-only); a separate Auth domain (staff identity belongs to Store, customer identity to Customers — domain-model §1.7, §5; shared mechanics are technical, in `Infrastructure`).

---

## 5. Backend modules and dependencies

### 5.1 Ownership

| Module | Owns (entities) | Owns invariants | Main operations |
|---|---|---|---|
| **Store** | Organization, Store, StoreDomain, StoreSettings, StoreBranding row, StaffUser, StaffPasswordCredential, StaffSession, StoreMembership, role → permission map, PlatformAuditEvent | INV-9 (admin side), INV-12 (status side), INV-13 (default-locale guard with Catalog) | Store resolution, effective status, **suspension guard** (`FOR SHARE` org → store, status from the lock result — P4-1), activation, suspension, settings, staff identity, audit |
| **Media** | MediaAsset (+ renditions) | INV-11 (with Ordering) | Upload pipeline (NetVips, ADR-10), reference tracking, cleanup |
| **Catalog** | Category, Product, Option, Variant, product images | INV-8, INV-13 (content) | Product save as one unit (with Inventory level creation), publish, archive |
| **Inventory** | InventoryLevel, StockMovement | INV-2 (guard), INV-10 (stock side) | Receive, Remove, Count, Contain; status-agnostic commit/release/deliver operations called by Ordering |
| **Customers** | Customer, CustomerCredential, CustomerSession, reset tokens | INV-9 (customer side) | Registration, login, sessions, password change/reset |
| **Cart** | Cart, CartLine, add-request records | INV-6 | Add (request id), set, remove, adjust, merge, quote |
| **Ordering** | Order, OrderLine, Payment, OrderEvent, OrderNumberSequence, guest access token | INV-3, INV-4, INV-5, INV-7, INV-10 (order side) | Placement, transitions, edits, cancellation, returns, payment actions, **reconciliation checks** (read-only, INV-2/3/4) |
| **ReadModels** | None | None | Combined read views |

### 5.2 Allowed dependencies

**[APPROVED]** domain graph (system-behavior §1); technical layers **[TECH]**:

```text
Hosts (Api, Worker, Cli) ─► ReadModels ─► Modules ─► Infrastructure ─► SharedKernel
Hosts ─► Persistence ─► Modules
Ordering ─► Cart, Catalog, Inventory, Customers, Media
Cart ─► Catalog, Inventory, Customers
Inventory ─► Catalog        Catalog ─► Store, Media        Media ─► Store        Customers ─► Store
Store ─► (no module)
```

**Forbidden** (checked by the compiler through project references, and by ArchUnitNET as a second check — M0-12):
- Any module → a module not below it in the graph (e.g., `Catalog → Ordering`, `Store → anything`).
- Modules → `ReadModels`, `Persistence`, or a host.
- `Infrastructure` / `SharedKernel` → any module.
- `Worker` → endpoint handlers; `Api` → `Worker`.

### 5.3 Cross-module operations

| Need | How |
|---|---|
| A lower module's data or rule used by a higher one | Direct call to the lower module's public operation, inside the caller's transaction |
| Suspension check for any prohibited write | Every module calls `Store`'s suspension guard first in its transaction (all such modules depend on Store directly or transitively) |
| W1 provisioning needs Store **and** Ordering rows | Orchestrated by the Cli `provision` handler (X-1) |
| A module must cause an email | Writes an outbox message (Infrastructure outbox writer) in its transaction; the Worker renders and sends |
| A lower module must react to a higher one's change | Not allowed by direct call. The higher module calls the lower one's operation explicitly (e.g., Ordering → Inventory commit/release/deliver — system-behavior W5/W6) |

### 5.4 Transaction ownership

**[TECH]**, consistent with ADR-02 and P4-1's EF Core rules:
1. The **entry point** — an endpoint handler, a CLI command, or a job — begins the transaction through the transaction runner, and is the only code that commits.
2. Operations called from another module **enlist** in the caller's transaction and never begin or commit one. They assert that a transaction is active.
3. One scoped `PlatformDbContext` per request/command/job; all explicit SQL runs through it (P4-1 EF rules 1–3).
4. Locks are taken in the global order (system-behavior §5.2) **before** `SaveChanges`.
5. The P4-2 audit write after a rollback is a new operation with its own scope and transaction.

---

## 6. Persistence architecture

### 6.1 One context type, model contributed by modules

**[TECH]** — resolves B-3.

- `Infrastructure` defines `PlatformDbContext`. It knows no entity types at compile time; `OnModelCreating` applies the `IEntityTypeConfiguration` classes of the registered module assemblies.
- Each module accesses its own entities through `context.Set<T>()`. A module **cannot** reference another module's entity types unless it may depend on that module, so the compiler still enforces the graph.
- `Persistence` composes the complete module set once (`PlatformModel.Assemblies`). Every host registers the same set, so all processes share one model, and that model is what migrations are generated from.
- Why not one context per module? Cross-module transactions (placement, product save, provisioning) would need several contexts sharing a connection, which contradicts P4-1 EF rule 1. Why not a context in each host? Modules could not use it.

### 6.2 Schema conventions

| Topic | Rule | Label |
|---|---|---|
| Primary keys | UUID v4 (`Guid.NewGuid()`) | [APPROVED] cross-cutting |
| Store scoping | `store_id NOT NULL` on store-scoped tables; composite foreign keys `(store_id, id)` for same-store references | [APPROVED] ADR-02 §2c |
| Naming | snake_case tables and columns; plural table names (as in milestone-m0-m1) | [TECH] |
| Case-insensitive SKU | Unique expression index `(store_id, lower(sku))`, created in migration SQL | [TECH] (domain rule approved) |
| Partial uniqueness | One `Active` cart per owner: unique `(store_id, customer_id) WHERE status = 'Active'`; one primary domain per store: unique `(store_id) WHERE is_primary` | [TECH] (rules approved) |
| Optimistic concurrency | Integer `version` columns as EF concurrency tokens; never `xmin` | [APPROVED] ADR-02 |
| Money | `bigint` minor units + currency code | [APPROVED] |
| Time | `timestamptz` (UTC), `date` for business dates | [APPROVED] |
| `LocalizedText` | `jsonb` with EF JSON mapping | [APPROVED] ADR-02 |
| Append-only tables | `platform_audit_events`, `stock_movements`, `order_events`: the application role has `INSERT`/`SELECT` only | [TECH] (M1 already requires it for audit) |
| Idempotency records | Unique constraints inserted with `INSERT … ON CONFLICT DO NOTHING` (explicit SQL) | [APPROVED] ADR-02 |
| Outbox | `outbox_messages`: type, payload `jsonb`, encrypted payload for link-bearing messages, attempts, `next_attempt_at`, status, timestamps; dispatched with `FOR UPDATE SKIP LOCKED` | [APPROVED] ADR-07; columns [TECH] |

### 6.3 Migrations

**[APPROVED]** EF Core migrations; generated SQL reviewed in the PR; CI applies migrations from scratch; production uses reviewed scripts or bundles, never `Database.Migrate()` at startup. **[TECH]** additions:
- CI runs `dotnet ef migrations has-pending-model-changes`, which fails if the model changed without a migration.
- Indexes, privileges, and role settings that EF cannot express are written as SQL inside the migration and reviewed the same way.

### 6.4 Database roles

**[TECH]**

| Role | Rights | Used by |
|---|---|---|
| `platform_migrator` | DDL | Migration bundle / CI |
| `platform_app` | DML, except `UPDATE`/`DELETE` on append-only tables; P4-7 (c) timeouts | Api, Cli, Worker |
| `platform_readonly` | `SELECT` only; longer statement timeout | Reconciliation (`REPEATABLE READ READ ONLY`) |

---

## 7. Security, tenancy, and persistence rules

### 7.1 Enforcement points

| Guarantee | Enforced at | Label | Tested by |
|---|---|---|---|
| Only the edge-received host identifies the store | Edge deletes and overwrites headers; SSR verifies the ingress key; API trusted-path middleware (credential + source network); API reads only `X-Platform-Host` | [APPROVED] ADR-04 | TH-1–TH-14 (M0), re-run with resolution (M1) |
| SSR cannot act beyond public reads | SSR-to-API key limited to an allow-list of public `GET`s; GET-only server-side interceptor | [APPROVED] ADR-04 | TH-5, TH-13, TH-14 |
| Unknown host → no store | Resolver returns `STORE_NOT_FOUND`; never a fallback | [APPROVED] | M1-2 |
| No cross-store reads/writes | (1) explicit `storeId` in every command and query; (2) EF global query filters as a safety net; (3) `store_id NOT NULL` + composite FKs; (4) explicit SQL always takes a `StoreId` parameter; (5) identical 404 for foreign and unknown ids | (1)–(3), (5) [APPROVED]; (4) [TECH] | Isolation harness on every store-scoped endpoint; schema isolation test; ArchitectureTests check that explicit-SQL methods take `StoreId` |
| Admin access | `StaffSession` scheme; membership for the route's `storeId` (none → 404); permission = role ∩ store-status filter (missing → 403); every admin endpoint **declares** a permission | [APPROVED] ADR-05 §5b | Authorization matrix (M2); an ArchitectureTests rule fails on an admin endpoint without a permission |
| Customer access | `CustomerSession` scheme, session bound to its store; a customer cookie never authenticates an admin request | [APPROVED] | M9 auth tests |
| Session invalidation | Rotation on login and password change; revocation on reset and disable; idle and absolute expiry | [APPROVED] domain-model §5.5 | M2 (staff), M9 (customers) |
| CSRF | Antiforgery bound to session and store; `X-XSRF-TOKEN` header; filter on every unsafe method; `Origin` check; JSON only | [APPROVED] ADR-05 §5d | M2 CSRF tests |
| Rate limiting | Per-IP built-in limiter + PostgreSQL identifier counters (HMAC keys) | [APPROVED] ADR-06 | M2 (staff login), M9 (customer flows) |
| Suspension | P4-1 guard in every prohibited write; D8 operations unguarded | [APPROVED] RA-6; P4-1 (PR #8) | SU-1–SU-8 |
| Guest order access and cancellation | Secret access token (hash stored); cancellation takes the shared suspension lock | [APPROVED] domain-model §7.11; system-behavior §5.1 | M8a |
| Operator audit | Succeeded: same transaction; Refused/Failed: P4-2 | [APPROVED] RA-1; P4-2 (PR #9) | M1-13, AU-1–AU-6 |
| Safe logging | `Microsoft.Extensions.Compliance.Redaction`; data classification for phones, emails, tokens, cookies, keys | [APPROVED] cross-cutting | M0-10 |
| Secrets | user-secrets and environment variables only; placeholders committed; each process gets only its own keys | [APPROVED] | M0-9, M0-16, TH-8, TH-14 |
| Frontend checks | UX only; the backend is authoritative | [APPROVED] ADR-03 | — |

### 7.2 Persistence consistency

Covered by the approved documents and placed in §6 and §11: unique constraints, conditional stock updates, explicit locks in the global order, request-id and idempotency-key records, the transactional outbox, append-only audit, and read-only snapshot reconciliation.

---

## 8. API conventions

| Topic | Rule | Label |
|---|---|---|
| Style | Minimal APIs; route groups as named in the domain-model contracts: `/api/storefront`, `/api/cart`, `/api/checkout`, `/api/auth` (customers), `/api/account`, `/api/orders` (guest lookup and access), `/api/admin/auth`, `/api/admin/me`, `/api/admin/stores/{storeId}`; plus `/api/csrf` (ADR-05) and `/api/health` (M0) | [APPROVED] ADR-01, domain-model API operations |
| Versioning | **No version segment in the MVP.** The only clients are the two first-party Angular apps, deployed with the API and typed from the same `openapi.json`; the drift check catches breaking changes at build time. Introduce `/api/v2` only if third-party clients appear | [TECH] |
| Errors | Problem Details with `code` (from `ErrorCodes`) and optional `details`; validation errors use `ValidationProblemDetails` with `code: VALIDATION_FAILED`; identical 404 bodies for unknown and foreign resources | [APPROVED] |
| Validation | Built-in Minimal API validation for simple constraints (flows into OpenAPI); FluentValidation for cross-field and domain rules; duplicated rules covered by shared test vectors | [APPROVED] ADR-03 |
| Authorization | Admin endpoints call `.RequirePermission("…")`; storefront write endpoints require the store resolved from the host and the CSRF filter | [APPROVED] ADR-05 (API shape [TECH]) |
| Caching | Default `private, no-store`; public endpoints opt in explicitly | [APPROVED] ADR-04 §4f |
| Idempotency | Request ids / idempotency keys in a header or body per contract; replays signal via `Idempotent-Replayed: true` | [APPROVED] mechanism; header [APPROVED] P4-3 |
| OpenAPI | Every endpoint declares success and error responses; integration tests validate responses against `openapi.json` | [APPROVED] ADR-03 |

---

## 9. Frontend structure

### 9.1 Workspace

**[APPROVED]** one Angular workspace, `storefront` (SSR) and `admin` (client-only), libraries for the design system, i18n/formatting, and generated contracts (requirements §24, ADR-03). **[TECH]** internal structure:

```text
frontend/projects/
├─ storefront/src/
│  ├─ main.ts  main.server.ts  server.ts      server.ts: ingress-key check, locale redirect (P4-4), per-locale bundle
│  ├─ app/
│  │  ├─ app.config.ts                         browser providers (no secrets)
│  │  ├─ app.config.server.ts                  server-only: SSR-to-API interceptor (ADR-04 rule 7)
│  │  ├─ app.routes.ts  app.routes.server.ts   render mode per route
│  │  ├─ core/                                 store context, locale and direction, error mapping, HTTP setup
│  │  └─ features/                             status-pages, shell (M1); catalog (M4); cart (M6); checkout (M7); order-tracking (M8a); account (M9)
│  └─ locale/                                  messages.ar.xlf, messages.en.xlf (T-1)
├─ admin/src/app/
│  ├─ core/                                    staff session state, XSRF, store context from the route, error mapping
│  ├─ shell/                                   navigation, store switcher (M2)
│  └─ features/                                auth, settings (M2); media, catalog (M3); inventory (M5); orders (M8a/b); dashboard (M10)
├─ design-system/                              tokens, primitives, patterns; storefront and admin entry points (ui-design §2)
├─ i18n/                                       money, number, date (Africa/Cairo), digit and phone normalization, direction helpers
└─ api-contracts/                              src/lib/schema.d.ts (generated) + http/ (Problem Details types, helpers)
```

### 9.2 Rules

| Topic | Rule | Label |
|---|---|---|
| Feature boundaries | A feature folder contains its routes, components, and **data-access** services (thin, typed with generated types). Features never import each other; shared code moves to `core/` or a library | [TECH] |
| API clients | Hand-written thin services per feature using `HttpClient` and generated types; no generated runtime client | [APPROVED] ADR-03 |
| Business rules | None in Angular beyond UX validation; duplicated rules use shared test vectors | [APPROVED] |
| Rendering | Catalog and status pages: server-rendered (no prerendering — host-dependent). Cart, checkout, account: client-rendered. SSR sets no cookies, forwards no browser cookies, and transfers no private data | [APPROVED] requirements §24, ADR-04 |
| Locale and direction | URL prefix `/ar/` or `/en/`; `<html lang dir>` per locale; logical CSS properties only (lint rule); mixed-language values get their own `lang`/`dir` | [APPROVED] requirements §6, ui-design §5 |
| UI strings | Angular built-in i18n, one build per locale | [APPROVED] T-1 |
| State | Signals + feature services | [APPROVED] T-2 |
| States | Every data view implements loading (skeleton), empty, error (Problem Details `code` → localized message), and unauthorized states. Admin: 401 → login, 403 → permission page, 404 → not found; `STORE_NOT_ACTIVE` → banner | [TECH] per ui-design §8 |
| Configuration | Browser builds contain no secrets and no internal URLs (relative `/api` only). The SSR server reads its two keys and `INTERNAL_API_BASE_URL` from environment variables, validated at startup, with no API edge key setting (TH-14) | [APPROVED] ADR-04 |
| Design system | Angular CDK for behavior, own components on tokens; Angular Material in admin only; Storybook | [PROPOSED] in ui-design §2 (Phase 5 review) |

---

## 10. Test strategy

**[APPROVED]** tools per ADR-11. Organization and timing **[TECH]**:

| Layer | Location | Tools | Can be added now (M0) | Added with the feature |
|---|---|---|---|---|
| Domain unit | `backend/tests/UnitTests/<Module>` | xUnit v3, Shouldly, CsCheck | Value types (`Money`, `Hostname` normalization, `Phone` vectors) | Each module's rules |
| PostgreSQL integration | `backend/tests/IntegrationTests` | `WebApplicationFactory`, Testcontainers, Respawn | Migrations from zero, role privileges, health/readiness, Problem Details, cache default | Constraints, locks, concurrency per milestone |
| Tenant isolation | IntegrationTests + `TestSupport` two-store builder | — | Harness skeleton | Every store-scoped endpoint, from M1 |
| Host spoofing | IntegrationTests + `frontend/e2e` | Playwright | TH-1–TH-14 (M0) | Re-run with resolution (M1) |
| Suspension races | IntegrationTests | Separate contexts/connections per actor | — | SU-1/2/6/7 (M1), SU-3–SU-5/SU-8 with each prohibited write (M2–M9) |
| Audit | IntegrationTests | Log capture, failure injection | — | AU-1–AU-6 (M1) |
| Auth and authorization | IntegrationTests | — | — | Matrix, CSRF, rotation/revocation (M2, M9) |
| Contract drift | CI script | `dotnet build`, `openapi-typescript` | M0 step 5 | Every endpoint |
| Architecture | `backend/tests/ArchitectureTests` | ArchUnitNET | Graph + violation fixture (M0-12) | Endpoint permission rule (M2), `StoreId` parameter rule (M3) |
| Angular unit/component | Inside each project | CLI default runner for the pinned major | Formatting library, placeholder components | Each feature |
| SSR and routing | `frontend/e2e` + server tests | Playwright | SSR health render (M0-2) | Locale redirects LR-1, status pages (M1) |
| E2E | `frontend/e2e` | Playwright through Caddy | M0 flows | Critical MVP flows (M7 guest checkout, M8a order handling, M9 accounts) |

**Never** the EF Core in-memory provider or SQLite for anything that depends on PostgreSQL semantics (ADR-11).

---

## 11. Transaction and locking matrix (P4-7 (d))

**[TECH]** — consolidates system-behavior §5 and W1–W12; each row is verified when its milestone is built.

| Workflow | Transaction owner | Locks (global order) | Concurrency token | Idempotency | Retry (P4-7 (b)) | Milestone / tests |
|---|---|---|---|---|---|---|
| Provisioning (W1) | Cli `provision` | — (new rows) | Unique handle, hostname | Unique handle; safe re-run | No (operator re-runs) | M1: M1-9, M1-10, AU-1/3 |
| Activation, suspension, reactivation (W1, W11) | Cli command | Store or org row **exclusive**, taken directly | Compare-and-set on status | Compare-and-set | No | M1: M1-12, SU-1/2/6 |
| Settings write (`names` group) | Admin endpoint | Org `FOR SHARE` → store row **exclusive** (P4-1 rule 6) | `version` | — | Yes | M2: SU-8 |
| Other settings / branding writes | Admin endpoint | Org → store `FOR SHARE` | `version` | — | Yes | M2 / B: SU-3 |
| Media upload | Admin endpoint | Org → store `FOR SHARE` | — | — | Yes (file stored before the transaction; orphan cleanup W12) | M3 |
| Product save (unit) | Admin endpoint | Org → store `FOR SHARE`; product row | `version` | — | Yes | M3: INV-8, all-or-nothing |
| Inventory adjustment (W8) | Admin endpoint | Inventory level exclusive (D8: no store lock) | Expected `onHand` (Count/Contain) | `(storeId, requestId)` + fingerprint | Yes | M5: IN-1–IN-9 |
| Cart write (W3) | Storefront endpoint | Org → store `FOR SHARE`; cart exclusive | — | `(cartId, requestId)` for add | Yes | M6: SU-3, replay tests |
| Placement (W5) | Checkout endpoint | Org → store `FOR SHARE`; cart; inventory levels ascending | Conditional stock update; quote fingerprint | `(storeId, idempotencyKey)` | Yes | M7: concurrency suite |
| Order transitions, staff cancel, payment actions, returns (W6) | Admin endpoint | Order (version); inventory levels ascending (D8: no store lock) | Order `version` | Compare-and-set | Yes | M8a |
| Customer/guest cancellation | Storefront endpoint | Org → store `FOR SHARE`; order; inventory | Order `version` | Compare-and-set | Yes | M8a/M9 |
| Item-edit apply (W7) | Admin endpoint | Org → store `FOR SHARE`; order; inventory ascending | `version` + preview fingerprint | Compare-and-set | Yes | M8b |
| Registration, login merge (W4) | Storefront endpoint | Org → store `FOR SHARE`; both carts ascending (merge) | — | Unique identifiers per store; merge carry-over of request records | Yes | M9 |
| Outbox dispatch (W12) | Worker | Message rows `FOR UPDATE SKIP LOCKED` | Status | At-least-once; provider idempotency key | Own backoff | M2 |
| Reconciliation | Worker / Cli trigger | None (`REPEATABLE READ READ ONLY`) | — | Read-only | Re-run | M10 |

---

## 12. Milestone plan

**[APPROVED]** milestone order and scope: [roadmap.md](roadmap.md) (approved 2026-10-19 as the change-controlled plan of record — PR #11) and [milestone-m0-m1.md](milestone-m0-m1.md) (M0/M1 scope approved). This section adds the structure, dependencies, and exit criteria; it does not move scope between milestones. Every milestone follows the approved slice shape (migration → backend operation with tests → OpenAPI → generated types → UI → E2E) and the [definition of done](milestone-m0-m1.md#definition-of-done-applies-to-m0-m1-and-every-later-milestone).

### M0 — Walking skeleton

| | |
|---|---|
| **Objective** | Prove the stack end to end, including the trusted-host path, with no business features |
| **Included** | milestone-m0-m1 §2; step order §1a (steps 2–7 remain) |
| **Excluded** | Business tables, auth, store resolution, styling, deployment |
| **Requires** | **G3** (owner); **T-1** before step 4; B-3/B-4 structure (§5, §6) for step 2 |
| **Creates** | `backend/` (all projects of §4 except `Worker`; modules empty), `frontend/` (both apps, libraries, `e2e/`), `contracts/`, `dev/caddy`, `scripts/`, `.github/workflows/ci.yml`, `dependabot.yml`, `README.md` |
| **Database** | Initial migration (no business tables), roles of §6.4 |
| **API / frontend** | Health endpoints, one example endpoint typed end to end; SSR page calling `/api/health` server-side; admin placeholder |
| **Security** | ADR-04 three credentials, diagnostics only in `Testing`, redaction, fail-fast configuration |
| **Tests** | M0-1–M0-17 (incl. TH-1–TH-14), architecture violation fixture, drift check, migrations from zero |
| **Exit** | All M0 criteria green in CI; required checks configured (M0-17); D-9 documentation fixes merged |

### M1 — Tenancy core

| | |
|---|---|
| **Objective** | Every store-host request resolves through the trusted path to exactly one store or none; lifecycle and operator commands with audit |
| **Included** | milestone-m0-m1 §3 |
| **Excluded** | Staff login, admin API, settings editing, commerce |
| **Requires** | PR #8 (P4-1) and PR #9 (P4-2) merged; **P4-4 confirmation**; M0 exit |
| **Creates** | `Modules/Store` (entities, resolver, effective status, suspension guard, audit), `Modules/Ordering` (sequence table only — M1-D2), `Cli` commands, `ReadModels` storefront config view, `storefront` status pages, shell, locale routing, SEO tags |
| **Database** | Tables in milestone-m0-m1 §3; composite keys; audit table privileges |
| **API / frontend** | `GET /api/storefront/config`; locale redirects (P4-4); status pages; `<html lang dir>` |
| **Security** | Resolution only from `X-Platform-Host`; effective status from the database for decisions; append-only audit |
| **Tests** | M1-1–M1-17; isolation harness; host-spoofing suite with resolution; schema isolation test; SU-1/2/6/7; AU-1–AU-6; LR-1 |
| **Exit** | All M1 criteria green; isolation harness reusable by M2 |

### M2 — Staff auth and admin authorization

| | |
|---|---|
| **Objective** | Staff can sign in to the admin host and act only within their memberships and permissions |
| **Included** | Roadmap M2: staff sessions, hashing (library vetted — ADR-05), Antiforgery + `Origin`, rate limiting, forced password change, `/admin/me`, membership 404 / permission 403 / status filter, Worker with outbox + Mailpit, staff password reset (revokes sessions), disable/enable commands, settings groups (no logo/theme), admin shell |
| **Excluded** | Customer auth (M9), branding (B) |
| **Requires** | **P4-7 (b), (c)**; **T-2**; ui-design admin shell reviewed (D-10) |
| **Creates** | Store: staff sessions and settings operations; `Infrastructure` security primitives, outbox writer, Data Protection store; `Worker` host; `admin` auth, shell, settings features |
| **Database** | `staff_sessions`, rate-limit counters, `outbox_messages`, Data Protection keys, Quartz tables |
| **Security** | `__Host-adm_session`, `SameSite=Strict`, CSRF on every unsafe method, rotation/revocation |
| **Tests** | Authorization matrix; CSRF suite; outbox exists iff committed; reset revokes all sessions; operator alerts (U-2); SU-8; endpoint-permission architecture rule |
| **Exit** | No admin endpoint without a declared permission; matrix green |

### M3 — Media and catalog admin

| | |
|---|---|
| **Objective** | Staff manage categories and products with images; product save is one unit with its inventory levels |
| **Included** | Roadmap M3 |
| **Excluded** | Storefront catalog pages (M4), inventory adjustments (M5) |
| **Requires** | M2; ADR-09/10 (approved); local MinIO |
| **Creates** | `Modules/Media`, `Modules/Catalog`, level creation in `Modules/Inventory`; admin media and catalog features |
| **Database** | Media assets and renditions; categories; products, options, variants (SKU expression index); inventory levels; movements |
| **Security** | Uploads through the backend, content-type detection, metadata stripping, 5 MB limit, cross-store media rejected, suspension guard on every write |
| **Tests** | INV-8 from both sides; all-or-nothing product save; cross-store media; SU-3 for catalog/media; `StoreId` architecture rule |
| **Exit** | Catalog admin usable on seeded stores |

### M4 — Storefront catalog

| | |
|---|---|
| **Objective** | Public, cached, localized catalog pages rendered on the server |
| **Included** | Roadmap M4 incl. the RA-7 query allow-list |
| **Excluded** | Cart (M6) |
| **Requires** | M3; ui-design storefront screens reviewed (D-10); P4-4 query rule from M4 |
| **Creates** | `ReadModels` catalog views; `storefront` catalog feature; SSR allow-list entries |
| **Security** | No quantities exposed; no cookies on cached responses; cache key = host + locale + path + allow-listed query |
| **Tests** | Isolation and host spoofing on every new endpoint; SEO/`hreflang`; cache headers; Core Web Vitals budget (ui-design §10) |
| **Exit** | Catalog pages pass isolation and caching gates |

### M5 — Inventory admin (parallel with M4 possible)

| | |
|---|---|
| **Objective** | Staff adjust stock safely with idempotent, audited operations |
| **Included** | Roadmap M5 |
| **Requires** | M3; **P4-3, P4-5, P4-6** |
| **Creates** | Inventory operations and endpoints; admin inventory feature |
| **Tests** | IN-1–IN-9; replay header (P4-3); property tests for INV-2 (CsCheck) |
| **Exit** | IN suite green on real PostgreSQL |

### M6 — Guest cart and quote

| | |
|---|---|
| **Objective** | Guests build a cart and see an authoritative quote |
| **Included** | Roadmap M6 |
| **Requires** | M4, M5 |
| **Creates** | `Modules/Cart`; storefront cart feature (client-rendered) |
| **Database** | Carts, lines, add-request records; partial unique index on the active cart |
| **Security** | `__Host-cart` cookie, `SameSite=Lax`; suspension guard on every cart write; foreign variant → identical 404 |
| **Tests** | INV-6; replay never double-increments; SU-3 for cart writes; SU-4 (concurrent cart writes not serialized) |
| **Exit** | Cart and quote E2E green |

### M7 — Guest checkout and placement

| | |
|---|---|
| **Objective** | Guests place cash-on-delivery orders without overselling |
| **Included** | Roadmap M7 |
| **Requires** | M6 |
| **Creates** | `Modules/Ordering` placement; outbox confirmation email template in `Worker`; storefront checkout and confirmation |
| **Database** | Orders, lines, payments, events, access tokens |
| **Tests** | Concurrency suite (last unit, double submit, lost response, suspension race); INV-2/3/4/7; guest checkout E2E |
| **Exit** | Concurrency suite green |

### M8a — Order lifecycle (admin) · M8b — Edits and corrections

| | |
|---|---|
| **Objective** | Staff run the full order lifecycle; edits and corrections are safe |
| **Included** | Roadmap M8a, M8b |
| **Requires** | M7 |
| **Tests** | Every W6 row as one test across five effect columns; D8 operations work while suspended (SU-5); stale version/fingerprint → 409, nothing written; edit cannot commit after suspension |
| **Exit** | Lifecycle and edit suites green |

### M9 — Customer accounts

| | |
|---|---|
| **Objective** | Customers register, sign in, see their orders, and keep their cart across login |
| **Included** | Roadmap M9 (parallel-work rules in roadmap §4) |
| **Requires** | M8a (and M2's auth mechanics) |
| **Creates** | `Modules/Customers`; storefront account feature |
| **Tests** | Merge idempotency; all sessions revoked on reset; no enumeration; SU-3 for registration and merge |
| **Exit** | Account E2E green |

### M10 — Dashboard, jobs, hardening

| | |
|---|---|
| **Objective** | Operational readiness of the MVP |
| **Included** | Roadmap M10 |
| **Requires** | M8, M9 |
| **Tests** | Reconciliation clean on seeded data; media cleanup never deletes order-referenced assets; accessibility checks; security review |
| **Exit** | MVP feature-complete locally |

### B — Branding (Phase 5)

Requires OQ1 / UQ1 approval and M3. Logo, theme tokens, store-branded emails and the 503 page.

### Milestone-independent gates

**[APPROVED]** roadmap §2: first shared environment (ADR-12, **U-5**, production email provider, TH-12 re-verified); commercial launch (OQ3/U-6, OQ2).

---

## 13. Next actions

In order:

1. **Owner:** configure G3 branch protection (B-1).
2. **Owner:** review and merge PR #8, then PR #9 (resolve the small textual overlap in the second); update milestone prerequisite P7 to show P4-2 approved.
3. **Owner decisions:** P4-3, P4-4, P4-6, P4-7 (b)(c), T-1, T-2, and the roadmap — **approved 2026-10-19** (PR #11). **P4-5 held** pending the owner's answer on the W8 `422` conflict (needed by M5).
4. Decisions recorded in system-behavior §7.4, the requirements decision log, and the affected texts — PR #11 (stacked on #9 → #8).
5. **M0 step 2:** create the backend structure of §4–§6.
