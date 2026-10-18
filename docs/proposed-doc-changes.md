# Proposed Documentation Changes — For Review

| | |
|---|---|
| **Status** | **Proposal — nothing below has been applied** |
| **Last updated** | 2026-10-18 |
| **Affects** | [requirements.md](requirements.md) v1.8, [domain-model.md](domain-model.md), [system-behavior.md](system-behavior.md) |

> **How to read this document.** Each item shows the **current text** (quoted exactly), the **source of truth** used to resolve it, the **proposed text**, and every affected section. The approved documents stay unchanged until you approve an item; approved items will then be applied, recorded in the requirements decision log, and requirements will move to **v1.9**.
>
> **Rule used for resolution:** the approved requirements and domain decisions are the source of truth. Where two approved texts disagree, the **later, more specific approved decision** wins, and the older text is updated — this is the update procedure requirements.md itself prescribes ("this document must be updated first — with a new entry in the Decision log").

**Contents**
1. [The four known conflicts (C1–C4)](#1-the-four-known-conflicts)
2. [Additional inconsistencies found during this review (A1–A5)](#2-additional-inconsistencies-found-during-this-review)
3. [Changes required by your decisions (D1 mock API, D2 tax)](#3-changes-required-by-your-decisions)
4. [RA-1 to RA-8 — full text for review](#4-ra-1-to-ra-8--full-text-for-review)
5. [Proposed decision-log entries](#5-proposed-decision-log-entries)

---

## 1. The four known conflicts

### C1 — Cache invalidation: on-demand vs. TTL-only

| | |
|---|---|
| **Current — domain-model §1.6** ("Storefront behavior while suspended") | "Server-side and CDN caches for the store are **invalidated** on every status change." |
| **Current — domain-model §1.8** (Storefront, step 4) | "Resolution results are cached per host and invalidated on domain, settings, or status changes." |
| **Conflicts with — system-behavior §4.5** | "**Time-based expiry only:** a maximum TTL of **60 seconds** for public catalog content; **no on-demand invalidation** in the MVP." |
| **Source of truth** | Requirements v1.8 decision log, 2026-10-18: "Locale prefix in URLs … TTL-only cache expiry"; requirements §23 "Caching". This later approved decision supersedes the earlier domain-model wording. |

**Proposed text**
- domain-model §1.6 → "Public pages reflect a status change within **60 seconds** (time-based cache expiry; no on-demand invalidation in the MVP — [system-behavior §4.5](system-behavior.md#45-invalidation-approved)). Transactions are blocked **immediately** by the backend, which always reads the current status from the database."
- domain-model §1.8 step 4 → "Resolution results are cached per host for **at most 60 seconds**; transactional checks never use this cache ([system-behavior §4.5](system-behavior.md#45-invalidation-approved))."

**Affected sections:** domain-model §1.6, §1.8. No requirements change.

---

### C2 — Cancellation reasons: one list vs. separate customer and staff lists

| | |
|---|---|
| **Current — requirements §11.1** (Rules) | "**Cancellation reasons:** customer changed mind, unreachable for confirmation, out of stock, duplicate order, other (with note)." |
| **Conflicts with — domain-model §7.6** | "**Reasons** — customer: changed my mind, ordered by mistake, other. Staff: customer request, unreachable for confirmation, out of stock, duplicate order, other (note)." |
| **Source of truth** | Domain-model §7 (approved 2026-10-18) is the later, more specific decision. The single requirements list cannot be right for customers: a customer cannot choose "unreachable for confirmation". |

**Proposed text — requirements §11.1**
> **Cancellation reasons**
> - *Customer or guest:* changed my mind, ordered by mistake, other (with note).
> - *Staff:* customer request, unreachable for confirmation, out of stock, duplicate order, other (with note).

**Proposed text — domain-model §7.6:** "… other (with note)" for **both** lists, so the two documents read identically.

**One point needs your decision:** the domain model writes customer "other" **without** "(note)". The proposal makes the note required for both, as the current requirements text does. If you prefer an optional note for customers, say so.

**Affected sections:** requirements §11.1; domain-model §7.6. Requirements §8.5.1 ("Cancel orders with reason `Out of stock`") stays valid (staff list).

---

### C3 — Item-edit apply: previewed grand total vs. preview fingerprint

| | |
|---|---|
| **Current — requirements §12 item 7.2** | "*Apply:* the admin confirms; the request includes the order version and the **previewed grand total**. If the order, prices, or stock changed since the preview, the apply is rejected and a new preview is shown." |
| **Conflicts with — domain-model §7.9** | "**Apply** (with order version and **preview fingerprint**; rejected if anything changed)" — also system-behavior W7 and §5.3. |
| **Source of truth** | Domain-model §7.9 (later). It also matches the approved cart decision (domain-model §6.6.2): a fingerprint is used "because offsetting changes can keep the total equal while the order differs". A grand-total comparison cannot detect "prices changed" as requirements §12.7 itself demands. |

**Proposed text — requirements §12 item 7.2**
> *Apply:* the admin confirms; the request includes the order version and the **preview fingerprint** (a hash of the previewed lines, prices, quantities, shipping, and totals). If the order, prices, or stock changed since the preview, the apply is rejected and a new preview is shown.

**Affected sections:** requirements §12. No domain-model or system-behavior change.

---

### C4 — Outdated document status headers

| Document | Current | Proposed |
|---|---|---|
| requirements.md | Status "v1.8 — Approved"; Phase "1 — Requirements Analysis (complete)"; Next phase "2 — Domain Analysis and Data Modeling" | Status "**v1.9 — Approved**" *(after these changes are approved)*; Phase "1 — Requirements Analysis (complete; maintained through later phases)"; Next phase "**4 — Architecture**" |
| domain-model.md | Status "In progress — Phase 2"; Based on "requirements.md v1.8" | Status "**Approved — core domains** (Branding and Theme: Phase 5)"; Based on "requirements.md **v1.9**" |
| system-behavior.md | Status "Final draft — pending approval"; Based on "requirements.md v1.8" | **Unchanged until you approve Phase 3.** Then: "Approved"; Based on "v1.9" |

Also: the requirements intro sentence "This is the single source of truth for Phase 2" → "… for all later phases".

---

## 2. Additional inconsistencies found during this review

These were not in the earlier list of four. Each needs the same review before the documents can be called consistent.

### A1 — Concurrency for Receive and Remove

| | |
|---|---|
| **Current — requirements §8.5** | "Adjustments use optimistic concurrency: the request includes the `onHand` value the admin saw; the backend rejects it if stock changed in the meantime." (applies to **all** adjustments) |
| **Conflicts with — domain-model §4.4** | Receive: "Delta update — always safe"; Remove: "Conditional delta update"; **only Count** uses the expected-value check. The domain model explains why: "Deltas are always correct under concurrency; only a count needs the expected-value check." |
| **Source of truth** | Domain-model §4.4 (later, with explicit rationale). |

**Proposed text — requirements §8.5:** "**Count** uses optimistic concurrency: the request includes the `onHand` value the admin saw, and the backend rejects it if stock changed in the meantime. Receive and Remove are delta operations and are safe under concurrency; Remove is still rejected if it would make `onHand < committed`."

### A2 — Count: expected `onHand` vs. `version`

| | |
|---|---|
| **Current — domain-model §4.4** | Count input: "the `onHand` value the admin saw"; rejected "if `onHand` changed since it was read". |
| **Current — domain-model §4.8** | "`version` … ✅ (for Count)" |
| **Issue** | Two different concurrency tokens are named for the same operation. System-behavior §5.3 and W8 use the expected `onHand`. |

**Proposed:** domain-model §4.8 → remove "(for Count)" from the `version` row, or remove the row if `version` has no other use on InventoryLevel. Count uses the expected `onHand` only. *(If you prefer `version`, system-behavior §5.3, W8, and domain-model §4.4 change instead.)*

### A3 — Permission for archiving categories

| | |
|---|---|
| **Current — domain-model §1.7** | "`catalog.read` / `catalog.write` / `catalog.publish` — Products, categories, images; publish/unpublish/archive" |
| **Current — domain-model §2.11** | "Categories: archive, restore — `catalog.write`" |
| **Issue** | §1.7 can be read as "archiving categories needs `catalog.publish`"; §2.11 says `catalog.write`. No impact in the MVP (StoreAdmin has all permissions), but endpoints declare one permission each. |

**Proposed:** domain-model §1.7 → "`catalog.publish` — publish, unpublish, archive and restore **products**; archive and restore **variants**. Category archive/restore uses `catalog.write` (§2.11)."

### A4 — Temporary password from an environment variable

| | |
|---|---|
| **Current — requirements §18.3** | The provisioning script "either generates a random temporary password … **or reads it from an environment variable or secret store**." |
| **Current — system-behavior W1 / domain-model §1.15** | Always "a random temporary password", displayed once after commit. |
| **Issue** | The requirements allow an operator-chosen password; the later documents do not. |

**Proposed (recommended):** keep both, made consistent — W1 and domain-model §1.15 say "a random temporary password, **or one supplied through an environment variable or secret store**; in both cases only the hash is stored, it is never logged, and `mustChangePassword` is set. A generated password is displayed once after commit." *(Alternative: remove the environment-variable option from requirements §18.3.)*

### A5 — Staff password reset: "provisioning script" vs. separate reset command

| | |
|---|---|
| **Current — requirements §18.4** | A forgotten staff password "can only be reset by the platform operator running the provisioning script, which issues a new temporary password". |
| **Conflicts with — system-behavior W1** | Provisioning with an existing `Active` email makes "**no credential change, no new temporary password**"; recovery uses "the **staff reset script** (audited `StaffPasswordReset`)". |
| **Source of truth** | System-behavior W1 / domain-model §1.15 (later; approved decision OPEN-8). |

**Proposed text — requirements §18.4:** "… can only be reset by the platform operator running the **staff password reset command**, which issues a new temporary password with 'must change password' set, revokes the user's sessions, and records a `StaffPasswordReset` platform audit event."

> *Note:* "revokes the user's sessions" is not stated anywhere today. It follows from the session invalidation policy (domain-model §5.13: old sessions must not survive a password reset). **Please confirm.**

---

## 3. Changes required by your decisions

### D1 — Drop the separate mock API track

Your decision: build end-to-end slices against the real backend; frontend mocks only as temporary test doubles, never a second source of truth.

| Section | Current text | Proposed |
|---|---|---|
| requirements §24, row "Mock API" | "A standalone HTTP server (not an in-browser mock), so SSR can call it and contracts stay realistic. API contracts are independent of the mock." | Row renamed **"Development approach"**: "Features are built as end-to-end slices against the real backend. Frontend test doubles may be used for isolated component and unit tests, but there is no separate mock API implementation. The API's OpenAPI document, generated from the backend, is the contract; Angular's contract types are generated from it ([architecture ADR-03](architecture-decisions.md#adr-03-repository-layout-and-api-contracts))." |
| requirements §11.4 item 7 | "**The mock API** simulates these rules for frontend development but does not provide real transactional guarantees. Correctness is the real backend's responsibility." | "Correctness of these rules is the backend's responsibility and is verified by backend tests against a real database; frontend test doubles never substitute for them." |
| requirements §18.3, last bullet | "**Mock API (development):** fake staff credentials come from a local, git-ignored environment file. The mock API is never deployed publicly." | "**Local development:** development stores and staff users are created with the same provisioning script, using values from a local, git-ignored environment file. No development credentials are committed or shared." |
| requirements §22 item 13 | "**Mock API is development-only** and provides no security guarantees." | "**Development tooling** (seed data, local proxies, test doubles) is never deployed and provides no security guarantees." |
| domain-model §3.4, last sentence | "The mock API does not need to implement it." | Delete the sentence. |
| requirements §24, row "Two apps, one workspace" | "One Angular workspace with two applications (`storefront`, `admin`) and shared libraries (design system, i18n/formatting, API contract types)." | **No change needed** (revised 2026-10-18). With a .NET backend, the "API contract types" library is generated from OpenAPI inside the Angular workspace, so the current text stays accurate. |
| requirements §24 — **new row** *(only if ADR-01 is approved)* | — | "**Backend** — C# on .NET (LTS) with ASP.NET Core; PostgreSQL. Details in [architecture-decisions.md](architecture-decisions.md)." |

### D2 — Tax: what `taxIncluded` means, and what changes

**What the documents currently say — two separate concepts**

| Concept | Where | Type | Meaning |
|---|---|---|---|
| `pricesIncludeTax` | StoreSettings (domain-model §1.5); requirements §14 | **Boolean store setting**, required, `true`, **read-only** in the MVP | A pricing **policy**: entered and displayed prices already contain tax. It is not configurable by staff; it is stored rather than hardcoded so future markets need configuration, not code. |
| `taxIncluded` | Order totals (domain-model §7.12); quote "Tax" row (§6.6) | **Money amount** | The **portion of the grand total that is tax**, "computed by the backend and stored in the order snapshot" (requirements §10.3, §14). |

So `taxIncluded` is **not** a setting and **not** a boolean — it is a computed amount. A boolean alone does not define tax behavior.

**Conflict with your decision.** Computing an included tax amount requires a rate (amount × rate ÷ (1 + rate)) — exactly the rate and extraction engine you excluded, and no rate is defined in any document. As written, requirements §10.3, §12 item 6, and §14 require a calculation the MVP should not have.

**Proposed resolution**
1. **`pricesIncludeTax`:** keep as is — stored, required, fixed to `true`, read-only. **No per-store choice** in the MVP; nothing in the requirements needs one.
2. **`taxIncluded`:** becomes **optional and absent in the MVP**, meaning "tax not itemized". The MVP computes no tax amount, holds no rate, and shows a fixed "Prices include tax" note on cart, checkout, order pages, and emails (wording finalized with the UI copy).
3. **Future-proofing:** proper tax rules arrive later as additive changes — for example a tax rule per store or category, with per-line `taxRate` and `taxAmount` snapshots. Orders placed in the MVP keep the field absent, which reads correctly forever as "not itemized" — no historical data needs rewriting.
4. **INV-4 is unchanged** (`grandTotal = subtotal + shippingFee` already ignores tax).

**Proposed text changes**

| Section | Current | Proposed |
|---|---|---|
| requirements §10.3, bullet | "Subtotal, tax amount (as computed by the backend), grand total, currency." | "Subtotal, shipping fee, grand total, currency. *(Tax is included in prices and not itemized in the MVP — Section 14.)*" |
| requirements §12 item 6 | "**Totals:** subtotal, shipping, tax, and grand total are recomputed by the backend." | "**Totals:** subtotal, shipping, and grand total are recomputed by the backend." |
| requirements §14 "Tax" | "Store setting `pricesIncludeTax = true` for all MVP stores: displayed prices include tax." / "**Tax rates and rules are owned by the backend.** No tax rate exists in frontend code." / "The tax amount shown on an order is computed by the backend and stored in the order snapshot." | "Store setting `pricesIncludeTax = true` for all MVP stores (read-only): prices are entered and displayed **including tax**." / "**The MVP does not calculate, extract, or itemize tax.** No tax rate exists anywhere in the system; cart, checkout, orders, and emails state that prices include tax." / "Itemized tax is a future, backend-owned capability; order totals reserve an optional tax amount for it ([domain-model §7.12](domain-model.md#712-entities))." |
| requirements §14 "Responsibilities" row | "Cart and order totals, shipping, tax — Backend" | "Cart and order totals, shipping — Backend" |
| domain-model §6.6 "Contents", Tax row | "Included tax amount (backend-computed); `pricesIncludeTax`" | "`pricesIncludeTax` (always `true`); no tax amount in the MVP" |
| domain-model §6.6.2, fingerprint | "a hash of its lines, quantities, unit prices, shipping, tax, and grand total" | "a hash of its lines, quantities, unit prices, shipping, and grand total" |
| domain-model §6.9, row | "Unit prices, line totals, subtotal, shipping, tax, grand total" | "Unit prices, line totals, subtotal, shipping, grand total" |
| domain-model §7.12, Totals | "`subtotal`, `shippingFee`, `taxIncluded`, `grandTotal`, `currency`" | "`subtotal`, `shippingFee`, `grandTotal`, `currency`; `taxIncluded?` — **reserved, absent in the MVP** (tax not itemized)" |
| system-behavior W7 step 3 | "… shipping with the **snapshotted rule**, tax, grand total, and a **preview fingerprint**." | "… shipping with the **snapshotted rule**, grand total, and a **preview fingerprint**." |

**Risk to record (not an MVP blocker):** Egyptian tax-invoicing or e-receipt rules may require itemized tax on documents a seller issues to consumers. The MVP issues no tax invoices or receipts; brands remain responsible for their tax documents. This should be reviewed with OQ2 **before a real launch**. *Proposed addition to requirements §26 as OQ3.*

---

## 4. RA-1 to RA-8 — full text for review

**Status of all eight: assumption — requires your approval.** None is confirmed.

**Important:** several are already written as rules in documents marked **approved** (column "Already stated as a rule in"). If you reject one, those sections must change as well.

### RA-1 — Operator identity

> **Operator identity:** there are no operator accounts in the MVP. Operator scripts require an operator identifier (argument or environment), recorded as the actor of each PlatformAuditEvent.

| | |
|---|---|
| Why it is needed | Audit events need an actor. |
| Implications | The identifier is **self-declared**, not authenticated: anyone able to run the scripts can enter any name. Accountability therefore rests on controlling who can run operator commands (infrastructure access). Operator accounts would mean building the deferred super-admin area early. |
| Already stated as a rule in | domain-model §1.14 (`actor` field, citing RA-1) |
| Used in | W1, W11; all operator CLI commands from M1 |

### RA-2 — Disabled staff identity during provisioning

> **Disabled staff identity during provisioning:** provisioning is refused; re-enabling the identity is a separate, audited operator action.

| | |
|---|---|
| Why it is needed | Reusing a disabled identity would silently re-grant access. |
| Implications | A separate operator command **"enable staff user"** is needed (`StaffUserEnabled` audit action already exists in domain-model §1.14). The operator runs two commands in this case. |
| Already stated as a rule in | domain-model §1.15 ("Found, `Disabled` → **Refused**", citing RA-2); system-behavior W1 step 2 and failure table |
| Used in | M1 (refusal); the enable command lands with staff management in M2 |

### RA-3 — Locale prefix for single-locale stores; no `Accept-Language`

> **Single-locale stores also use the locale prefix**, and `/` redirects to the default locale **without** using `Accept-Language`.

| | |
|---|---|
| Why it is needed | Uniform routing; deterministic, cacheable redirects. |
| Implications | An Arabic-only store's URLs look like `/ar/...`. A visitor whose browser prefers English always lands on the store's default locale first and switches manually. Changing a store's default locale later changes where `/` points (relevant to redirect caching — see M1 scope). |
| Already stated as a rule in | requirements §6 "Locale in URLs" states "Every storefront page URL starts with its locale" and "`/` redirects to the store's default locale" — the **single-locale** and **no-`Accept-Language`** parts appear only in system-behavior §4.3. |
| Used in | **M1** (routing) — must be decided before M1 starts |

### RA-4 — Replayed add-to-cart returns the current cart

> **Replayed add-to-cart returns the current cart**, not a stored copy of the original response. Add-request records live as long as their cart.

| | |
|---|---|
| Why it is needed | The cart may have changed since; nothing extra to store. |
| Implications | A client must not assume a replay response shows only the effect of its own add. Request records are deleted with the cart (cart cleanup job). |
| Already stated as a rule in | domain-model §6.4 ("A repeat returns the **current cart**"), §6.10 (CartAddRequest "deleted together with its cart") |
| Used in | M6 |

### RA-5 — Count and Contain have no request id

> **Count and Contain do not require a request id**: the expected-`onHand` check already rejects repeats.

| | |
|---|---|
| Why it is needed | Avoids an unnecessary extra mechanism. |
| Implications | A repeated Count usually gets `409` (stale) and the UI must reload and show the current value. **Edge case:** if the counted value equals the value the admin saw, a repeat passes the check again and records a second, harmless `Counted` movement with no stock change. Acceptable, or add a request id to Count — your call. |
| Already stated as a rule in | domain-model §4.4 ("Count and Contain rely on the expected-`onHand` check instead") |
| Used in | M5 |

### RA-6 — No store-status lock for cart writes, registrations, staff actions

> **Cart writes, registrations, and staff actions are not locked against store-status changes**; one racing a suspension by milliseconds is accepted, since none creates an order.

| | |
|---|---|
| Why it is needed | Keeps the store row out of hot paths. |
| Implications | A cart line or a registration may commit a few milliseconds after a suspension commits. No order, stock commitment, or payment can result (placement is serialized). |
| Already stated as a rule in | requirements §22 item 16 ("only order placement (shared) and store/organization status changes (exclusive) lock the store or organization row") |
| Used in | M2, M6, M9 |

### RA-7 — Allow-listed query parameters in cache keys

> **Cache keys use an allow-list of query parameters** (`page`, `sort`, `q`, …); tracking parameters are ignored.

| | |
|---|---|
| Why it is needed | Fewer cache variants; resists cache poisoning. |
| Implications | Every public page parameter that changes content must be added to the allow-list, or it will be ignored by the cache. The list is maintained alongside routes. |
| Already stated as a rule in | Only system-behavior §4.4 |
| Used in | M1 (header rules), M4 (catalog parameters), ADR-12 (shared cache configuration) |

### RA-8 — Database lock primitives confirmed in Phase 4

> **Database lock primitives** (shared/exclusive row locks, conditional updates) are confirmed in Phase 4 against the chosen database.

| | |
|---|---|
| Why it is needed | Behavior in system-behavior is database-independent. |
| Implications | Becomes **confirmable** when ADR-02 is approved: PostgreSQL provides every primitive used ([architecture — concurrency spike assessment](architecture-decisions.md#concurrency-spike-assessment)). If another database is chosen, this must be reassessed. |
| Already stated as a rule in | requirements §11.4 item 5 ("a backend implementation concern") |
| Used in | M7, M8 |

---

## 5. Proposed decision-log entries

To be added to requirements §28 **only for items you approve**:

| Date | Decision |
|---|---|
| *(approval date)* | Cache invalidation is TTL-only (≤ 60 s) everywhere; domain-model §1.6 and §1.8 aligned (C1) |
| *(approval date)* | Separate cancellation reasons for customers/guests and staff (C2) |
| *(approval date)* | Item-edit apply verified by preview fingerprint, not grand total (C3) |
| *(approval date)* | Only Count uses the expected-`onHand` check; Receive/Remove are deltas (A1, A2) |
| *(approval date)* | Category archive/restore uses `catalog.write` (A3) |
| *(approval date)* | Temporary staff password: generated or supplied via a secret; staff reset is a separate audited command (A4, A5) |
| *(approval date)* | No separate mock API; end-to-end slices against the real backend; shared contracts library (D1) |
| *(approval date)* | Tax included in prices and not itemized in the MVP; `pricesIncludeTax` fixed and read-only; order tax amount reserved (D2) |
| *(approval date)* | RA-n confirmed *(one entry per approved assumption)* |
| *(approval date)* | **Requirements v1.9 approved** |
