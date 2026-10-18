# Documentation Change Record — v1.9 Draft (Architecture Review)

| | |
|---|---|
| **Status** | **Applied to the working drafts — pending approval.** Nothing in this record is approved. |
| **Last updated** | 2026-10-18 |
| **Affects** | [requirements.md](requirements.md) (v1.8 → **v1.9 draft**), [domain-model.md](domain-model.md), [system-behavior.md](system-behavior.md), [architecture-decisions.md](architecture-decisions.md), [milestone-m0-m1.md](milestone-m0-m1.md) |

> **How to read this record.** Each item lists the **previously approved text** (quoted), the **change applied in the draft**, the **affected sections**, and its **status**. The applied text is visible in the documents themselves, marked *pending approval*. If you reject an item, the quoted previous text is restored.
>
> **Resolution rule:** the approved requirements and domain decisions are the source of truth; where two approved texts disagreed, the later, more specific approved decision was kept and the older text updated. Review adjustments from 2026-10-18 are noted per item.

**Contents**
1. [Known conflicts C1–C4](#1-known-conflicts-c1c4)
2. [Additional inconsistencies A1–A5](#2-additional-inconsistencies-a1a5)
3. [Decision-driven changes D1–D2](#3-decision-driven-changes-d1d2)
4. [RA-1 to RA-8 — revised text, pending approval](#4-ra-1-to-ra-8--revised-text-pending-approval)
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
| **Status** | Applied — pending approval |

### C2 — Cancellation reasons and note validation

| | |
|---|---|
| **Previous text** | requirements §11.1: "**Cancellation reasons:** customer changed mind, unreachable for confirmation, out of stock, duplicate order, other (with note)." · domain-model §7.6: "customer: changed my mind, ordered by mistake, other. Staff: customer request, unreachable for confirmation, out of stock, duplicate order, other (note)." |
| **Applied** | Separate lists for customer/guest and staff (domain-model wording). **Review adjustment:** a note is **required for "other"** for every actor (1–500 characters *[DEFAULT]*), optional otherwise; reasons outside the actor's list are rejected. Same rule for delivery-failure reasons. |
| **Sections** | requirements §11.1; domain-model §7.6; system-behavior W6 |
| **Status** | Applied — pending approval |

### C3 — Item-edit apply protection

| | |
|---|---|
| **Previous text** | requirements §12 item 7.2: "the request includes the order version and the **previewed grand total**." |
| **Applied** | Apply carries the **order version and the preview fingerprint**; the backend recomputes inside the transaction; a matching total alone is never sufficient. |
| **Sections** | requirements §12 |
| **Status** | Applied — pending approval |

### C4 — Status and phase labels

| Document | Previous | Applied |
|---|---|---|
| requirements.md | "v1.8 — Approved"; Phase 1 complete; Next phase 2 | "**v1.9 — Draft, pending approval** (last approved: v1.8)"; Current phase 4 (in review) |
| domain-model.md | "In progress — Phase 2"; based on v1.8 | "Core domains approved · architecture-review revisions applied in draft, pending approval"; based on v1.9 draft |
| system-behavior.md | "Final draft — pending approval"; based on v1.8 | Unchanged status (still pending); based on v1.9 draft |
| architecture-decisions.md | Based on v1.8 | Based on v1.9 draft; revision 3 |

**Review adjustment:** labels changed only after the edits were applied; nothing is labelled approved. **Status:** applied — pending approval.

---

## 2. Additional inconsistencies A1–A5

### A1 — Receive/Remove are deltas; Count uses the expected value

| | |
|---|---|
| **Previous text** | requirements §8.5: "Adjustments use optimistic concurrency: the request includes the `onHand` value the admin saw…" (all adjustments) |
| **Applied** | Receive/Remove = delta operations; Count (absolute) and Contain use the expected `onHand`; Remove and Count can never leave `onHand < committed`. |
| **Sections** | requirements §8.5; system-behavior W8 |
| **Status** | Applied — pending approval |

### A2 — No inventory `version` contract

| | |
|---|---|
| **Previous text** | domain-model §4.2: InventoryLevel "…discrepancy marker, version"; §4.8: "`version` … ✅ (for Count)" |
| **Applied** | `version` removed from InventoryLevel and its contract; Count/Contain send the expected `onHand` instead. |
| **Sections** | domain-model §4.2, §4.8 |
| **Status** | Applied — pending approval |

### A3 — Category archive/restore permission

| | |
|---|---|
| **Previous text** | domain-model §1.7: "`catalog.publish` … Products, categories, images; publish/unpublish/archive" |
| **Applied** | `catalog.publish` = products (publish, unpublish, archive, restore) and variants (archive/restore); categories archive/restore with `catalog.write` (as §2.11). |
| **Sections** | domain-model §1.7 |
| **Status** | Applied — pending approval |

### A4 — Temporary passwords are always generated

| | |
|---|---|
| **Previous text** | requirements §18.3: the script "either generates a random temporary password … **or reads it from an environment variable or secret store**." |
| **Applied** | **Review adjustment** (replaces the earlier recommendation to keep both): only **generated** temporary passwords (secure generator), shown once after successful provisioning, never logged or persisted in plaintext. The externally supplied option is removed — no requirement needs it. |
| **Sections** | requirements §18.3; domain-model §1.15; system-behavior W1; milestone M1-11 |
| **Status** | Applied — pending approval |

### A5 — Staff password reset separate from provisioning; sessions revoked

| | |
|---|---|
| **Previous text** | requirements §18.4: reset "by the platform operator running the provisioning script, which issues a new temporary password…" |
| **Applied** | A separate **staff password reset command**: generated temporary password (shown once), `mustChangePassword`, **all staff sessions revoked**, `StaffPasswordReset` audit event. |
| **Sections** | requirements §18.4; domain-model §1.15, §5.4, §5.5; system-behavior W1 |
| **Status** | Applied — pending approval |

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
| requirements §24 new "Backend" row | — | Added, marked *proposed — pending approval of ADR-01/02* |

**Status:** applied — pending approval.

### D2 — Tax-inclusive pricing without tax calculation

| | |
|---|---|
| **Previous text** | requirements §10.3 "Subtotal, tax amount (as computed by the backend)…"; §12 item 6 "…shipping, tax, and grand total…"; §14 "The tax amount shown on an order is computed by the backend…"; domain-model §6.6 "Included tax amount (backend-computed)", §6.6.2 fingerprint "…shipping, tax, and grand total", §6.9 "…shipping, tax, grand total", §7.12 totals "`taxIncluded`"; system-behavior W7 step 3 "…tax, grand total…" |
| **Applied** | Prices are tax-inclusive (unchanged). `pricesIncludeTax` stays read-only `true` and is documented as a **policy flag only**, not a basis for calculation. **No tax amount** is calculated, stored, returned, or itemized; the fingerprint excludes tax. **Review adjustment:** `taxIncluded` is **removed** from order totals rather than reserved, to avoid implying a calculation; a future tax engine adds per-line tax snapshots for new orders only. Launch-readiness note added as **OQ3**. |
| **Sections** | requirements §10.3, §12, §14, §26; domain-model §6.6, §6.6.2, §6.9, §7.12; system-behavior W7 |
| **Status** | Applied — pending approval |

---

## 4. RA-1 to RA-8 — revised text, pending approval

The current wording is in [system-behavior §7.2](system-behavior.md#72-remaining-assumptions--pending-approval). The table records what changed. **None is approved.**

| # | Previous wording (summary) | Change applied | Status |
|---|---|---|---|
| RA-1 | Operator identifier recorded as actor | Added: a **self-declared audit label, not verified proof of identity** | Pending approval |
| RA-2 | Disabled identity → provisioning refused; re-enable separately | Unchanged in substance | Pending approval |
| RA-3 | Locale prefix also for single-locale stores; `/` → default locale without `Accept-Language` | Unchanged; now also stated in requirements §6 | Pending approval |
| RA-4 | Replay returns the current cart | Now stated in the **API contract** of `POST /api/cart/lines` (domain-model §6.11) | Pending approval |
| RA-5 | "Count and Contain do **not** require a request id" | **Reversed:** Count and Contain **require** a request id per `(storeId, requestId)`; replay returns the original result; different payload rejected; expected-`onHand` check kept as a separate protection | Pending approval |
| RA-6 | "Cart writes, registrations, and **staff actions** are not locked against store-status changes" | **Revised:** placement, **item-edit apply**, and **customer/guest cancellation** take shared organization/store locks; cart writes, registrations, catalog/settings writes stay unlocked; operations allowed while suspended need no synchronization | Pending approval — see note below |
| RA-7 | Allow-listed cache query parameters | Clarified: configured with the shared cache, first needed in **M4**; **not an M1 prerequisite** (M1 only requires query-independent public responses) | Pending approval |
| RA-8 | Lock primitives confirmed in Phase 4 | Now names **PostgreSQL** and requires integration tests against **real PostgreSQL** | Pending approval (depends on ADR-02) |

**Note on RA-6 — interpretation to confirm.** The review asked that "order placement and inventory/payment-affecting workflows must not proceed after suspension takes effect". Approved decision **D8** (domain-model §1.6) deliberately **allows** many inventory- and payment-affecting staff operations while suspended (fulfillment transitions, staff cancellation, mark as paid, payment correction, return recording, stock adjustments). The draft therefore applies the rule to operations that are **both** forbidden while suspended **and** affect stock or payment: placement, item-edit apply, customer/guest cancellation. **If you intended to block the D8 operations as well, D8 itself must change** — please confirm.

---

## 5. Architecture-review items R1–R4

| # | Item | Applied | Sections | Status |
|---|---|---|---|---|
| R1 | **Trusted host flow** (blocker) | Complete edge → SSR → API flow with separate edge and SSR keys, SSR-key route allow-list, host taken only from `X-Platform-Host`, test-only diagnostics, tests TH-1–TH-10; production edge capability open (U-5) | ADR-04; requirements §22 item 17; domain-model §1.8; system-behavior §4.4, W2; milestone M0-4–M0-8, M1-4 | Proposed — pending approval |
| R2 | **Order lifecycle** | Failed attempt = no status change (order stays `Shipped`); `DeliveryFailed` terminal, no retry; stock released only by cancellation, deducted at `Shipped`, restored only by return recording; attempt recording open (OQ4 / U-1) | requirements §11.1, §26; domain-model §7.3, §7.4, §7.7, §7.14; system-behavior W6 | Applied — pending approval |
| R3 | **Suspended-store caching** | Suspended `503`: `no-store` + `Retry-After` (fixes an internal contradiction in system-behavior §4.2, which listed it as cacheable while also stating 5xx are never cached); responses default to `private, no-store` unless explicitly public | system-behavior §4.2, §4.5, W2, W11; domain-model §1.6; requirements §23; milestone M0-15, M1-7 | Applied — pending approval |
| R4 | **Inventory idempotency tests** | Processing order (lock → request-id lookup → validation → apply) and integration tests IN-1–IN-9 | system-behavior W8; domain-model §4.4, §4.10 | Applied — pending approval |

---

## 6. Unresolved decisions

Recorded in [system-behavior §7.3](system-behavior.md#73-unresolved-decisions):

| # | Decision | Recommendation |
|---|---|---|
| U-1 | Recording failed delivery attempts (OQ4) | Internal note (`NoteAdded`) in the MVP |
| U-2 | Operator alert channel | Error log + email to a configured operator address |
| U-3 | `Retry-After` value | 3600 s *[DEFAULT]* |
| U-4 | "Other" note length | 1–500 characters *[DEFAULT]* |
| U-5 | Production edge capability for ADR-04 | Verify with ADR-12; fallback mutual TLS |
| U-6 | Tax and invoicing obligations (OQ3) | Review before commercial launch |
| — | RA-6 interpretation vs. D8 | See §4 note |
| — | ADR-10 MediaAsset renditions (domain-model addition) | Not applied; waits for ADR-10 approval |
