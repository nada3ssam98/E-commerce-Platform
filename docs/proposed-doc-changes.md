# Documentation Change Record — v1.9 (Architecture Review)

| | |
|---|---|
| **Status** | **Reviewed 2026-10-18.** All items approved **except R1**: ADR-04 was rejected as written and is corrected in revision 4, **pending review**. U-5 remains open. |
| **Last updated** | 2026-10-18 |
| **Affects** | [requirements.md](requirements.md) (v1.8 → **v1.9, approved**), [domain-model.md](domain-model.md), [system-behavior.md](system-behavior.md), [architecture-decisions.md](architecture-decisions.md), [milestone-m0-m1.md](milestone-m0-m1.md), [roadmap.md](roadmap.md) |

> **How to read this record.** Each item lists the **previously approved text** (quoted), the **change applied**, the **affected sections**, and its **status**. The applied text is visible in the documents themselves.

## Review decisions — 2026-10-18

| Item | Decision |
|---|---|
| Architecture revision 3 | Approved **except ADR-04** (mandatory correction — see R1) |
| Requirements v1.9; C1–C4, A1–A5, D1–D2, R2–R4 | **Approved** |
| R1 / ADR-04 | **Not approved as written.** The SSR server received the same edge key the API accepted for all routes. Correction applied in ADR-04 **revision 4** — **pending review** |
| RA-1–RA-8 | **Approved**; RA-6 interpreted **narrowly** — D8 preserved (see §4) |
| U-1–U-4, U-6 | **Approved** as recommended |
| U-5 | **Open** until production hosting/edge capability is verified; resolve before the first shared environment |
| M1-D1–M1-D5 | **Approved** as proposed |
| ADR-11 toolchain | **Approved**; Node/Angular chosen from the official compatibility table at M0 start, not Node 26 merely because it is LTS |
| Tax/invoicing | Remains a **commercial-launch gate**; no tax calculation in the MVP |
>
> **Resolution rule:** the approved requirements and domain decisions are the source of truth; where two approved texts disagreed, the later, more specific approved decision was kept and the older text updated. Review adjustments from 2026-10-18 are noted per item.

**Contents**
1. [Known conflicts C1–C4](#1-known-conflicts-c1c4)
2. [Additional inconsistencies A1–A5](#2-additional-inconsistencies-a1a5)
3. [Decision-driven changes D1–D2](#3-decision-driven-changes-d1d2)
4. [RA-1 to RA-8 — approved 2026-10-18](#4-ra-1-to-ra-8--approved-2026-10-18)
5. [Architecture-review items R1–R4](#5-architecture-review-items-r1r4)
6. [Unresolved decisions](#6-unresolved-decisions)

---

## 1. Known conflicts C1–C4

### C1 — Cache invalidation: TTL-only

| | |
|---|---|
| **Previous text** | domain-model §1.6: "Server-side and CDN caches for the store are **invalidated** on every status change." · domain-model §1.8 step 4: "Resolution results are cached per host and invalidated on domain, settings, or status changes." |
| **Conflicted with** | system-behavior §4.5 (TTL ≤ 60 s, no on-demand invalidation); requirements decision log 2026-10-18 |
| **Applied** | TTL-only (≤ 60 s) everywhere; **business operations always read the current status from the database**, never a cache; the suspended `503` page is never cached (see R3). |
| **Sections** | domain-model §1.6, §1.8; system-behavior §4.5; requirements §23 |
| **Status** | Approved 2026-10-18 |

### C2 — Cancellation reasons and note validation

| | |
|---|---|
| **Previous text** | requirements §11.1: "**Cancellation reasons:** customer changed mind, unreachable for confirmation, out of stock, duplicate order, other (with note)." · domain-model §7.6: "customer: changed my mind, ordered by mistake, other. Staff: customer request, unreachable for confirmation, out of stock, duplicate order, other (note)." |
| **Applied** | Separate lists for customer/guest and staff (domain-model wording). **Review adjustment:** a note is **required for "other"** for every actor (1–500 characters *[DEFAULT]*), optional otherwise; reasons outside the actor's list are rejected. Same rule for delivery-failure reasons. |
| **Sections** | requirements §11.1; domain-model §7.6; system-behavior W6 |
| **Status** | Approved 2026-10-18 |

### C3 — Item-edit apply protection

| | |
|---|---|
| **Previous text** | requirements §12 item 7.2: "the request includes the order version and the **previewed grand total**." |
| **Applied** | Apply carries the **order version and the preview fingerprint**; the backend recomputes inside the transaction; a matching total alone is never sufficient. |
| **Sections** | requirements §12 |
| **Status** | Approved 2026-10-18 |

### C4 — Status and phase labels

| Document | Previous | Applied |
|---|---|---|
| requirements.md | "v1.8 — Approved"; Phase 1 complete; Next phase 2 | "**v1.9 — Approved** (2026-10-18)"; Current phase 4 |
| domain-model.md | "In progress — Phase 2"; based on v1.8 | "Approved — core domains, including the architecture-review revisions"; based on v1.9 |
| system-behavior.md | "Final draft — pending approval"; based on v1.8 | "All review items approved (2026-10-18)"; open: U-5 and ADR-04 revision 4; formal Phase 3 sign-off after the ADR-04 review; based on v1.9 |
| architecture-decisions.md | Based on v1.8 | Based on v1.9; revision 4 (ADR-04 pending review; others approved) |

**Review adjustment:** labels changed only after the edits were applied and the decisions recorded. **Status:** approved 2026-10-18.

---

## 2. Additional inconsistencies A1–A5

### A1 — Receive/Remove are deltas; Count uses the expected value

| | |
|---|---|
| **Previous text** | requirements §8.5: "Adjustments use optimistic concurrency: the request includes the `onHand` value the admin saw…" (all adjustments) |
| **Applied** | Receive/Remove = delta operations; Count (absolute) and Contain use the expected `onHand`; Remove and Count can never leave `onHand < committed`. |
| **Sections** | requirements §8.5; system-behavior W8 |
| **Status** | Approved 2026-10-18 |

### A2 — No inventory `version` contract

| | |
|---|---|
| **Previous text** | domain-model §4.2: InventoryLevel "…discrepancy marker, version"; §4.8: "`version` … ✅ (for Count)" |
| **Applied** | `version` removed from InventoryLevel and its contract; Count/Contain send the expected `onHand` instead. |
| **Sections** | domain-model §4.2, §4.8 |
| **Status** | Approved 2026-10-18 |

### A3 — Category archive/restore permission

| | |
|---|---|
| **Previous text** | domain-model §1.7: "`catalog.publish` … Products, categories, images; publish/unpublish/archive" |
| **Applied** | `catalog.publish` = products (publish, unpublish, archive, restore) and variants (archive/restore); categories archive/restore with `catalog.write` (as §2.11). |
| **Sections** | domain-model §1.7 |
| **Status** | Approved 2026-10-18 |

### A4 — Temporary passwords are always generated

| | |
|---|---|
| **Previous text** | requirements §18.3: the script "either generates a random temporary password … **or reads it from an environment variable or secret store**." |
| **Applied** | **Review adjustment** (replaces the earlier recommendation to keep both): only **generated** temporary passwords (secure generator), shown once after successful provisioning, never logged or persisted in plaintext. The externally supplied option is removed — no requirement needs it. |
| **Sections** | requirements §18.3; domain-model §1.15; system-behavior W1; milestone M1-11 |
| **Status** | Approved 2026-10-18 |

### A5 — Staff password reset separate from provisioning; sessions revoked

| | |
|---|---|
| **Previous text** | requirements §18.4: reset "by the platform operator running the provisioning script, which issues a new temporary password…" |
| **Applied** | A separate **staff password reset command**: generated temporary password (shown once), `mustChangePassword`, **all staff sessions revoked**, `StaffPasswordReset` audit event. |
| **Sections** | requirements §18.4; domain-model §1.15, §5.4, §5.5; system-behavior W1 |
| **Status** | Approved 2026-10-18 |

---

## 3. Decision-driven changes D1–D2

### D1 — Drop the separate mock API track

| Section | Previous text | Applied |
|---|---|---|
| requirements §24 "Mock API" row | "A standalone HTTP server (not an in-browser mock)…" | Row "Development approach": end-to-end slices against the real backend; test doubles only for isolated tests; OpenAPI-generated contract types |
| requirements §11.4 item 7 | "**The mock API** simulates these rules…" | Correctness verified by backend tests against real PostgreSQL |
| requirements §18.3 last bullet | "**Mock API (development):** fake staff credentials…" | Local development uses the provisioning script; no shared credentials |
| requirements §22 item 13 | "**Mock API is development-only**…" | "Development tooling … is never deployed" |
| domain-model §3.4 | "The mock API does not need to implement it." | Sentence removed |
| requirements §24 "Two apps, one workspace" | — | No change needed |
| requirements §24 new "Backend" row | — | Added; approved 2026-10-18 with ADR-01/02 |

**Status:** approved 2026-10-18.

### D2 — Tax-inclusive pricing without tax calculation

| | |
|---|---|
| **Previous text** | requirements §10.3 "Subtotal, tax amount (as computed by the backend)…"; §12 item 6 "…shipping, tax, and grand total…"; §14 "The tax amount shown on an order is computed by the backend…"; domain-model §6.6 "Included tax amount (backend-computed)", §6.6.2 fingerprint "…shipping, tax, and grand total", §6.9 "…shipping, tax, grand total", §7.12 totals "`taxIncluded`"; system-behavior W7 step 3 "…tax, grand total…" |
| **Applied** | Prices are tax-inclusive (unchanged). `pricesIncludeTax` stays read-only `true` and is documented as a **policy flag only**, not a basis for calculation. **No tax amount** is calculated, stored, returned, or itemized; the fingerprint excludes tax. **Review adjustment:** `taxIncluded` is **removed** from order totals rather than reserved, to avoid implying a calculation; a future tax engine adds per-line tax snapshots for new orders only. Launch-readiness note added as **OQ3**. |
| **Sections** | requirements §10.3, §12, §14, §26; domain-model §6.6, §6.6.2, §6.9, §7.12; system-behavior W7 |
| **Status** | Approved 2026-10-18 |

---

## 4. RA-1 to RA-8 — approved 2026-10-18

The current wording is in [system-behavior §7.2](system-behavior.md#72-approved-assumptions-ra-1-to-ra-8). The table records what changed. **All eight approved on 2026-10-18.**

| # | Previous wording (summary) | Change applied | Status |
|---|---|---|---|
| RA-1 | Operator identifier recorded as actor | Added: a **self-declared audit label, not verified proof of identity** | Approved 2026-10-18 |
| RA-2 | Disabled identity → provisioning refused; re-enable separately | Unchanged in substance | Approved 2026-10-18 |
| RA-3 | Locale prefix also for single-locale stores; `/` → default locale without `Accept-Language` | Unchanged; now also stated in requirements §6 | Approved 2026-10-18 |
| RA-4 | Replay returns the current cart | Now stated in the **API contract** of `POST /api/cart/lines` (domain-model §6.11) | Approved 2026-10-18 |
| RA-5 | "Count and Contain do **not** require a request id" | **Reversed:** Count and Contain **require** a request id per `(storeId, requestId)`; replay returns the original result; different payload rejected; expected-`onHand` check kept as a separate protection | Approved 2026-10-18 |
| RA-6 | "Cart writes, registrations, and **staff actions** are not locked against store-status changes" | **Revised:** placement, **item-edit apply**, and **customer/guest cancellation** take shared organization/store locks; cart writes, registrations, catalog/settings writes stay unlocked; operations allowed while suspended need no synchronization | Approved 2026-10-18 — narrow interpretation (see note) |
| RA-7 | Allow-listed cache query parameters | Clarified: configured with the shared cache, first needed in **M4**; **not an M1 prerequisite** (M1 only requires query-independent public responses) | Approved 2026-10-18 |
| RA-8 | Lock primitives confirmed in Phase 4 | Now names **PostgreSQL** and requires integration tests against **real PostgreSQL** | Approved 2026-10-18 |

**RA-6 — narrow interpretation (approved 2026-10-18).** D8 is **preserved**. Suspension blocks operations that must be prohibited while suspended, including new order placement, order item-edit apply, and customer/guest cancellation (these synchronize with suspension through shared organization/store locks). Existing-order fulfillment and the staff operations explicitly permitted by D8 remain available while suspended, subject to permissions, workflow rules, stock invariants, and audit logging.

---

## 5. Architecture-review items R1–R4

| # | Item | Applied | Sections | Status |
|---|---|---|---|---|
| R1 | **Trusted host flow** (blocker) | Revision 3 (one edge key forwarded to both SSR and API) **rejected**. Revision 4: three separate credentials — API edge key (edge → API only), SSR ingress key (edge → SSR only; rejected by the API), SSR-to-API key (allow-listed public `GET`s only); each bound to its source network; tests TH-1–TH-14; production edge capability open (U-5) | ADR-04; requirements §22 item 17; domain-model §1.8; system-behavior §4.4, W2; milestone M0-4–M0-8d, M1-4 | Requirement approved; **ADR-04 revision 4 pending review** |
| R2 | **Order lifecycle** | Failed attempt = no status change (order stays `Shipped`), recorded as an internal note (U-1); `DeliveryFailed` terminal, no retry; stock released only by cancellation, deducted at `Shipped`, restored only by return recording | requirements §11.1, §26; domain-model §7.3, §7.4, §7.7, §7.14; system-behavior W6 | Approved 2026-10-18 |
| R3 | **Suspended-store caching** | Suspended `503`: `no-store` + `Retry-After` (fixes an internal contradiction in system-behavior §4.2, which listed it as cacheable while also stating 5xx are never cached); responses default to `private, no-store` unless explicitly public | system-behavior §4.2, §4.5, W2, W11; domain-model §1.6; requirements §23; milestone M0-15, M1-7 | Approved 2026-10-18 |
| R4 | **Inventory idempotency tests** | Processing order (lock → request-id lookup → validation → apply) and integration tests IN-1–IN-9 | system-behavior W8; domain-model §4.4, §4.10 | Approved 2026-10-18 |

---

## 6. Unresolved decisions

Recorded in [system-behavior §7.3](system-behavior.md#73-review-decisions-and-open-items):

| # | Decision | Outcome | Status |
|---|---|---|---|
| U-1 | Recording failed delivery attempts (OQ4) | Internal note (`NoteAdded`) in the MVP | Approved 2026-10-18 |
| U-2 | Operator alert channel | Error log + email to a configured operator address | Approved 2026-10-18 |
| U-3 | `Retry-After` value | 3600 s *[DEFAULT]* | Approved 2026-10-18 |
| U-4 | "Other" note length | 1–500 characters *[DEFAULT]* | Approved 2026-10-18 |
| U-5 | Production edge capability for ADR-04 (per-route secrets, private origins, source binding) | Verify with ADR-12; fallback mutual TLS | **Open** — before the first shared environment |
| U-6 | Tax and invoicing obligations (OQ3) | Commercial-launch gate; no tax calculation in the MVP | Approved 2026-10-18 (gate) |
| — | RA-6 interpretation vs. D8 | Narrow interpretation; D8 preserved (§4) | Approved 2026-10-18 |
| — | ADR-10 MediaAsset renditions | Applied to domain-model §3.2 after ADR-10 approval | Approved 2026-10-18 |
| — | **ADR-04 revision 4** | Separate credentials and network binding | **Pending review** — implementation gate G1 |
