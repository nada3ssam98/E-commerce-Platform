# White-Label E-commerce Platform — Requirements (MVP)

| | |
|---|---|
| **Status** | **v1.9 — Approved** (2026-10-18) |
| **Last updated** | 2026-10-18 |
| **Phase** | 1 — Requirements Analysis (complete; maintained through later phases) |
| **Current phase** | 4 — Architecture (in review; see [architecture-decisions.md](architecture-decisions.md)) |

> **How to use this document.** This is the single source of truth for all later phases. If a design or implementation decision contradicts this document, either the decision is wrong or this document must be updated first — with a new entry in the [Decision log](#28-decision-log).
>
> **v1.9 (approved 2026-10-18):** the architecture-review changes are recorded in the [change record](proposed-doc-changes.md) and the [decision log](#28-decision-log). **Pending:** the RA-5 and RA-6 revisions from the second review of RA-1–RA-8 (2026-10-18) are marked *pending approval* in the text ([change record §7](proposed-doc-changes.md#7-ra-1-to-ra-8--second-review-2026-10-18)). The trusted-host **design** for Section 22 item 17 is [ADR-04](architecture-decisions.md#adr-04-request-topology-host-resolution-and-public-caching) revision 4, approved 2026-10-18.
>
> Items marked **[DEFAULT]** were accepted as defaults and can be changed cheaply. Everything else is approved. Remaining assumptions are listed in [Section 25](#25-assumptions).
>
> This is a requirements document, not a schema. Entity names are used to make rules precise; fields, types, and tables are decided in Phase 2.

---

## Table of contents

1. [Product overview](#1-product-overview)
2. [Glossary](#2-glossary)
3. [Users and roles](#3-users-and-roles)
4. [MVP scope](#4-mvp-scope)
5. [Platform and tenancy](#5-platform-and-tenancy)
6. [Localization](#6-localization)
7. [Catalog](#7-catalog)
8. [Inventory](#8-inventory)
9. [Cart](#9-cart)
10. [Checkout and order placement](#10-checkout-and-order-placement)
11. [Order status and payment status](#11-order-status-and-payment-status)
12. [Order editing](#12-order-editing)
13. [Shipping](#13-shipping)
14. [Pricing, money, and tax](#14-pricing-money-and-tax)
15. [Addresses](#15-addresses)
16. [Customer accounts and authentication](#16-customer-accounts-and-authentication)
17. [Guest order access](#17-guest-order-access)
18. [Staff accounts and admin access](#18-staff-accounts-and-admin-access)
19. [Notifications](#19-notifications)
20. [Admin dashboard](#20-admin-dashboard)
21. [Admin capabilities](#21-admin-capabilities)
22. [Security requirements](#22-security-requirements)
23. [Non-functional requirements](#23-non-functional-requirements)
24. [Provisional architecture decisions](#24-provisional-architecture-decisions)
25. [Assumptions](#25-assumptions)
26. [Open questions](#26-open-questions)
27. [Deferred features](#27-deferred-features)
28. [Decision log](#28-decision-log)

---

## 1. Product overview

A white-label, multi-tenant e-commerce platform delivered as **shared SaaS**: one running system and one codebase serve many brands. Each brand gets its own storefront with its own identity, theme, catalog, customers, and orders, and manages it through a brand admin dashboard.

**Goals**
- Serious learning/portfolio project, designed so it can become a real product.
- Adding a brand requires **data and configuration only** — never code changes, forks, or brand-specific components.
- Initial market: **Egypt** — Arabic and English (RTL and LTR), EGP, cash on delivery.

**MVP application areas**
1. **Customer storefront** — browse, search, cart, checkout, order tracking, customer accounts.
2. **Brand admin dashboard** — overview, products, categories, orders, inventory, store settings.

The platform super-admin area has **no UI** in the MVP; stores are provisioned through seed data or a backend process.

---

## 2. Glossary

| Term | Meaning |
|---|---|
| **Organization** | The business that is the platform's customer. Holds staff and (later) billing. |
| **Store** | A brand's storefront. **The data-isolation boundary**: all commerce data belongs to exactly one store. |
| **Tenant context** | The store a request operates on. Always means "the current store". |
| **Staff user** | A person who manages a store through the admin dashboard. |
| **Store membership** | The relationship granting a staff user access to a store. |
| **Customer** | A registered shopper account, scoped to one store. |
| **Guest** | A shopper who checks out without an account. |
| **Product** | What a shopper browses: content, images, category, base price. Not purchasable by itself. |
| **Option / option value** | A variant dimension (Size) and its values (S, M, L). |
| **Variant** | The **purchasable unit**: one combination of option values, with its own SKU, optional price, and stock. |
| **Effective price** | `variant.price` if set, otherwise `product.basePrice`. |
| **onHand** | Physical units in stock for a variant. |
| **committed** | Units promised to open orders and not yet shipped. |
| **available** | `onHand − committed`. What the storefront can sell. |
| **Open order** | An order in status `Pending`, `Confirmed`, or `Processing`. |
| **Snapshot** | Data copied into an order at placement time so later catalog or settings changes never alter it. |
| **Minor units** | Integer amount in the currency's smallest unit (piastres for EGP). |

---

## 3. Users and roles

| Role | Description | MVP |
|---|---|---|
| **Guest shopper** | Browses, adds to cart, checks out without an account. | ✅ |
| **Registered customer** | Store-scoped account; order history; persistent cart. | ✅ |
| **Store admin (staff)** | Manages one store through the admin dashboard. Single role in the MVP. | ✅ |
| **Platform operator** | Provisions stores and staff via seed data/scripts. No UI. | ✅ (no UI) |
| Staff roles (owner, employee, …) | Granular permissions. | Deferred |
| Platform super-admin UI | Onboard and manage brands through a UI. | Deferred |

---

## 4. MVP scope

### In scope

**Storefront**
- Home, category listing, product details with variant selection.
- Basic product search within the current store.
- Server-side cart for guests and customers.
- Checkout with cash on delivery; guest checkout.
- Order confirmation; order tracking (account history or guest lookup).
- Customer registration, login, logout, password recovery flow.
- Arabic and English, RTL and LTR, per the store's enabled locales.

**Admin dashboard**
- Overview: summary metrics and recent orders.
- Products: list, search, create, edit, publish/unpublish, archive; translations, images, options, variants, prices, stock.
- Categories: list, create, edit, archive; translations; two levels.
- Orders: list, search, details, status transitions, item editing, cancellation, payment status, return recording.
- Inventory: stock levels per variant, adjustments, low-stock warnings.
- Store settings: name, logo, basic theme, locales, base currency (EGP), shipping fee and free-shipping threshold.

### Out of scope
See [Deferred features](#27-deferred-features).

---

## 5. Platform and tenancy

1. **Organization → Store.** An organization owns stores; a store belongs to exactly one organization.
2. **One store per organization in the MVP.** This is a business rule, not a structural limit.
3. **Store is the isolation boundary.** Every product, variant, category, inventory record, customer, cart, order, and setting belongs to exactly one store.
4. **Stores are fully isolated** from each other, even within the same organization: no shared catalog, customers, carts, or orders. *(Assumption A1.)*
5. **Cross-reference rule:** any reference between store-owned records must point to a record in the **same store** (e.g., a cart line's variant, a product's category). The backend validates this on every write.
6. **Tenant resolution**
   - Storefront: resolved by the backend from the request **host name** via a host → store mapping (subdomains in the MVP, custom domains later).
   - Admin: resolved from the **store ID in the route**, verified against the staff user's memberships on every request.
7. **The frontend never provides security.** Guards and hidden UI are UX only; the backend enforces isolation and authorization.
8. **Store provisioning** in the MVP happens through seed data or a backend script that creates: organization, store, host mapping, settings, theme, staff user, and membership.
9. **Store names:** each store has a canonical **brand name** (not translated) and a localized **display name** (required in the default locale, prefilled from the brand name). Details in [domain-model §1.4](domain-model.md#14-store-names).
10. **Store lifecycle:** `Setup → Active ⇄ Suspended`, changed by the platform operator. A suspended store accepts no new carts, orders, or registrations and its catalog and settings are read-only; staff can still fulfill existing orders, and customers keep read-only access to their order history and order status. Full behavior in [domain-model §1.6](domain-model.md#16-store-lifecycle).
11. **Public support contacts:** support phone (required), support email (optional), WhatsApp number (optional). Shown in the storefront footer, on order pages, and in cancellation messages. They are store settings, separate from customer and recipient contact details.
12. **Staff authorization** has three separate layers — identity (staff user), access (store membership), and capability (role → permissions, restricted by store status) — enforced by the backend on every request. Details in [domain-model §1.7](domain-model.md#17-identity-membership-and-permissions).

---

## 6. Localization

### Store locale configuration
1. Supported platform locales: `ar`, `en`.
2. Each store has `supportedLocales` (non-empty subset of `{ar, en}`) and a `defaultLocale` that must be one of them.
3. A store may be Arabic-only, English-only, or bilingual.

### UI text vs. store content
- **UI text** (buttons, messages, validation) is provided by the platform in both locales.
- **Store content** (product names, descriptions, option names and values, category names) is entered by each store, per enabled locale.

### Required content
- A translatable **required** field must have a non-empty value in the store's **default locale**. Whitespace-only counts as empty.
- Other enabled locales are optional.

### Fallback rule
For each translatable field, independently:
1. Use the requested locale if the store supports it and the value is non-empty.
2. Otherwise use the store's default locale.

Because the default-locale value is required, resolved required content is **never empty**. Optional fields (e.g., a description) may be absent in both, in which case the UI omits them rather than showing an empty block.

### API behavior
- **Storefront API** returns content resolved to a single locale, plus the locale each value actually came from, so the UI can set `lang`/`dir` on mixed-language text.
- **Admin API** returns all translations for editing.

### Locale configuration changes
- Changing `defaultLocale` is **blocked** while any non-archived product or category lacks required content in the new default; the admin is shown what is missing.
- Removing a locale from `supportedLocales` hides that content but does not delete it.

### Locale in URLs
- Every storefront page URL starts with its locale: `/ar/...` or `/en/...` — **including single-locale stores**. *[RA-3, approved 2026-10-18]*
- `/` redirects to the store's default locale **without** using `Accept-Language`, so the redirect is deterministic; unsupported locale prefixes redirect to the same path in the default locale. *[RA-3, approved 2026-10-18]*
- Each page declares its canonical URL (own locale, primary host) and `hreflang` alternates for the store's supported locales plus `x-default`.
- **Redirect and cache behavior are deterministic:** the redirect depends only on the resolved store's default locale and the path. Cache keys include the resolved **store identity, locale, path**, allow-listed query parameters, and any other response-affecting context; one store's cached response is never served to another. *[RA-3, RA-7 — clarified in the second review, 2026-10-18]* Details in [system-behavior §4.3–4.4](system-behavior.md#43-locale-in-urls-approved).

### Direction and formatting
- `<html lang dir>` reflects the active locale.
- The design system uses logical CSS properties; directional icons mirror in RTL.
- Numbers use Western digits in both locales by default. *[DEFAULT]*
- Input fields accept Arabic-Indic digits (e.g., in phone numbers) and normalize them.

---

## 7. Catalog

### 7.1 Products
- A product has: translatable name (required), translatable description (optional), base price, one primary category, images, 0–3 options, and ≥1 variant.
- **Publication status:** `Draft`, `Published`, `Archived`.
  - `Draft ↔ Published` (publish/unpublish).
  - `Draft` or `Published → Archived`; `Archived → Draft` (restore).
- **Availability** (in stock / out of stock) is derived from inventory and is **separate** from publication status.
- Products are **archived, never hard-deleted**, because orders and history reference them.
- Only `Published` products are visible on the storefront.

### 7.2 Publishing rules
A product can be published only if **all** hold:
1. Name present in the store's default locale.
2. A primary category that is **not archived**.
3. At least one active (non-archived) variant.
4. Every active variant has an effective price > 0.
5. Options and variants pass the validation rules in 7.3.

### 7.3 Options and variants
1. A product has **0 to 3 options** (e.g., Size, Color, Material), ordered.
2. Each option has ≥1 ordered values. Option names and values are translatable.
3. A product with **0 options** has **exactly one default variant**, hidden from shoppers.
4. A variant has **exactly one value from each of its product's options** — no missing values, no duplicates, no values from another product.
5. **No two variants of a product may have the same combination.**
6. Combinations may be **sparse**: not every combination must exist. The storefront shows non-existent or unavailable combinations as unavailable.
7. Variants are **archived, not deleted**. Archived variants cannot be added to carts; open orders containing them are still fulfilled.
8. An option value used by an active variant cannot be removed; archive those variants first.

**Business constraint: option dimensions lock after first publication**
- While a product has **never been published**, options may be added, removed, and reordered freely.
- Once a product has been published at least once — even if later unpublished — its **option dimensions are locked**: options cannot be added or removed. Values may still be added, renamed, translated, and reordered; variants may be added for new combinations or archived.
- A structural change after publishing requires archiving the product and creating a new one.
- *Rationale:* carts, orders, and images reference specific combinations. Adding a dimension would leave existing variants incomplete; removing one would create duplicate combinations.
- Existing orders are unaffected by any catalog change because order items are snapshots (Section 10.4).

### 7.4 SKU
- Required for every variant (including the default variant).
- Unique **per store**, compared case-insensitively.

### 7.5 Pricing on the catalog
- `effectivePrice = variant.price ?? product.basePrice`.
- Changing `basePrice` changes the effective price of every variant without its own price. The admin UI labels such variants "inherits base price".
- The backend computes effective prices; the storefront receives resolved prices and never applies the fallback itself.
- Product listings show **"From X"** where X is the lowest effective price among purchasable variants.

### 7.6 Images
- A product has an ordered image gallery.
- An image may optionally be linked to an **option value** (e.g., `Color = Red`), applying to every variant with that value. Unlinked images form the general gallery.
- Images have translatable alt text (optional; falls back per Section 6).
- Limits: up to 10 images per product; JPEG, PNG, or WebP; up to 5 MB each. *[DEFAULT]*

### 7.7 Categories
1. **Two levels maximum:** top-level categories and their subcategories. A subcategory's parent must be top-level; a category with subcategories cannot become a subcategory.
2. Translatable name (required in default locale).
3. A product has **one primary category**, which may be a top-level category or a subcategory.
4. A top-level category's storefront listing includes products assigned to it **and** to its subcategories.
5. Categories are **archived, never deleted**.

**Archiving rules**
- A category **cannot be archived** while it has:
  - non-archived subcategories, or
  - `Published` products assigned to it.
- `Draft` and `Archived` products may remain assigned to an archived category:
  - They keep the reference for history and admin visibility.
  - A `Draft` product in an archived category **cannot be published** until reassigned to a non-archived category (publishing rule 7.2.2).
  - The admin product list flags such products ("category archived").
- Archived categories are hidden from the storefront.
- **Restoring** a subcategory requires its parent to be non-archived.

### 7.8 Search *[DEFAULT]*
- **Storefront:** matches published product names in the requested locale and the store's default locale.
- **Admin:** matches product name (any enabled locale) and variant SKU.
- Full-text search, filters, and facets are deferred.

### 7.9 Slugs
- Products and categories have **Latin slugs**, unique **within their store**.
- Product slugs are suggested from the English name, or from the SKU if no English name exists. Category slugs are suggested from the English name; otherwise the admin enters one.
- The admin can edit a slug until it is **locked at first publication**. Changing locked slugs and redirecting old slugs are **deferred**.
- Details in [domain-model §2.6](domain-model.md#26-slugs).

### 7.10 Descriptions
Plain text with preserved line breaks; never rendered as HTML. Translatable with the standard fallback. Rich text is deferred.

### 7.11 Storefront stock indicators
- Published products stay **visible when sold out**, with a **Sold out** state; sold-out or unavailable variants cannot be added to the cart.
- Shoppers see **states only** — `In stock`, `Low stock` (available ≤ the store's low-stock threshold, default 5), `Sold out`. Exact quantities are never exposed on product pages or listings.
- **Single exception:** when a cart line requests more units than are available (`0 < available < requested`), the cart may show the available quantity and offer **"Adjust to available"**, which the backend computes at submit time ([domain-model §6.6](domain-model.md#66-the-quote)).
- Indicators are informational; the backend revalidates stock on every cart change and at order placement.

### 7.12 Media uploads
- Product images and store logos are uploaded as store-scoped **media assets** before being attached; uploads can happen before the product exists.
- A store can never attach another store's media.
- Unused uploads are cleaned up automatically; media referenced by order history is never deleted.
- Details in [domain-model §3](domain-model.md#3-media).

---

## 8. Inventory

### 8.1 Model
Stock is tracked **per variant**:

| Quantity | Meaning | Changed by |
|---|---|---|
| `onHand` | Physical units in stock | Admin adjustments, shipping, return recording |
| `committed` | Units promised to open orders | Order placement, editing, cancellation, shipping |
| `available` | `onHand − committed` | Derived, never stored independently |

### 8.2 Invariants (enforced by the backend at all times)
1. `onHand ≥ 0`
2. `committed ≥ 0`
3. `committed ≤ onHand` → therefore `available ≥ 0`
4. **`committed` of a variant = the sum of quantities of that variant across all open orders** (`Pending`, `Confirmed`, `Processing`). This is the definition of "reserved"; it can be verified at any time with a reconciliation query.

### 8.3 Stock effects by event

| Event | Precondition | `onHand` | `committed` |
|---|---|---|---|
| Order placed (→ `Pending`) | `available ≥ qty` for **every** line (all-or-nothing) | — | `+ qty` |
| Line quantity increased `a → b` | Order editable; `available ≥ b − a` | — | `+ (b − a)` |
| Line quantity decreased `a → b` | Order editable; `b ≥ 1` | — | `− (a − b)` |
| Line removed | Order editable; ≥1 line remains | — | `− a` |
| Line variant changed `V1 → V2` | Order editable; `V2.available ≥ qty` | — | V1 `− qty`, V2 `+ qty` |
| Cancelled from `Pending`/`Confirmed`/`Processing` | Valid transition | — | `− qty` (all lines) |
| `Processing → Shipped` | Valid transition | `− qty` | `− qty` |
| `Shipped → Delivered` | Valid transition | — | — |
| `Shipped → DeliveryFailed` | Valid transition | — | — (goods are out of the warehouse) |
| Return recorded (after `DeliveryFailed`) | Not yet recorded for this order | `+ restockQty` per line | — |
| Admin stock adjustment | New `onHand ≥ committed` | set/adjust | — |

### 8.4 Preventing double reservation
1. Stock changes happen **only** as part of an order operation (placement, edit, transition, return) or an admin adjustment — never independently.
2. Each operation is **atomic** and protected against concurrent updates, as defined in [Section 11.4](#114-atomicity-and-concurrency).
3. Status transitions are **compare-and-set**: the backend changes status only if the order is still in the expected "from" status. A repeated or concurrent request finds the status already changed and fails without touching stock.
4. Order placement requires an **idempotency key** generated per checkout attempt. A retried or double-submitted checkout returns the original order instead of creating a second one.
5. Return recording is allowed **once per order**.

### 8.5 Admin stock adjustments
- Admins change **`onHand` only**, through three explicit operations: **Receive (+N)**, **Remove (−N)** (reason required), and **Count (set X)**. `committed` is never edited manually. Details in [domain-model §4.4](domain-model.md#44-admin-adjustments).
- An optional **initial quantity** may be given when a variant is first created, recorded as a `Received` movement in the same transaction as the product save ([domain-model §4.5](domain-model.md#45-initial-stock-on-variant-creation)).
- Reasons: `Restock`, `Initial stock`, `Stock count correction`, `Damaged/lost`, `Other` (with note).
- **All stock adjustments are idempotent** — Receive, Remove, Count, and Contain (8.5.1). Each carries a client-generated **request id**, unique per **`(storeId, requestId)`** and enforced by the server in the same transaction. Repeating a request id with the **same payload** returns the original result without applying the adjustment again or creating another stock movement; reusing it with a **different payload** is rejected. For Count and Contain, a stale expected `onHand` is rejected with a conflict that returns the current values; every applied adjustment writes exactly one stock movement, while replays and rejected attempts write none ([system-behavior W8](system-behavior.md#w8-inventory-adjustments-and-discrepancies)). *(Count and Contain request ids: RA-5, approved 2026-10-18; clarified in the second review 2026-10-18 — pending approval.)*
- An adjustment that would make `onHand < committed` is **rejected** (Remove and Count). *(MVP constraint: keeps `available` non-negative at all times.)* The rejection starts the discrepancy workflow (8.5.1).
- **Receive and Remove are delta operations** and are safe under concurrent stock changes. **Count sets an absolute value** and therefore uses optimistic concurrency: the request includes the `onHand` value the admin saw, and the backend rejects it if stock changed in the meantime. Containment uses the same expected-value check.
- Idempotency and the expected-value check are **separate protections**: the request id prevents a retried request from applying twice; the expected value prevents a count based on stale data from overwriting newer stock.

#### 8.5.1 Inventory discrepancy workflow
A **discrepancy** exists when the real physical count is lower than the units already committed to open orders (e.g., items found damaged or missing during a count).

1. **Detect.** The admin submits a count `X` where `X < committed`. The backend rejects it without changing stock and returns:
   - variant, current `onHand`, `committed`, submitted count `X`;
   - **shortage** = `committed − X`;
   - the **affected open orders** containing the variant: order number, status, quantity, placement date.
2. **Contain (optional, recommended).** The admin may immediately **stop selling** the variant by setting `onHand = committed` (the lowest allowed value), making `available = 0`. This is logged as `Stock count correction` with the counted value `X` and the shortage, and marks the variant with an **unresolved discrepancy**. Like Count, containment includes the `onHand` value the admin saw and a request id (8.5).
3. **Resolve through orders.** The admin, usually after contacting the affected customers, frees committed units using normal order operations:
   - **Edit** `Pending`/`Confirmed` orders (reduce quantity or swap variant) — Section 12.
   - **Cancel** orders with reason `Out of stock` — `Processing` orders can only be cancelled, not edited.
   - Each operation releases committed units and is logged in both the order event log and the stock movement log.
   - *UI guidance (not a rule):* affected orders are listed newest first, since newer orders are usually the fairest to adjust.
4. **Record the true count.** Once `committed ≤ X`, the admin submits the count `X` again; it now succeeds and **clears** the unresolved-discrepancy marker.
5. **Visibility.** Variants with an unresolved discrepancy are flagged in the inventory list and counted in the dashboard's low-stock card until resolved.

There is **no** way to bypass this workflow (e.g., force `committed` down) in the MVP.

### 8.6 Failed delivery and returns
- `Shipped → DeliveryFailed` does not change stock (goods are with the courier).
- When goods physically come back, the admin **records the return** once per order, choosing per line how many units to restock (`0..qty`). Restocked units increase `onHand`; the remainder is recorded as written off.

### 8.7 Stock movement log
Every change to `onHand` or `committed` is recorded in an append-only log with: variant, **before and after values** of `onHand` and `committed`, reason (and note), counted value for stock counts, related order (if any), actor (staff, customer, guest, system), and timestamp.

Stock movements are written in the **same transaction** as the change they describe; a stock change without its log entry cannot exist.

### 8.8 Low stock
- Each variant is in exactly one state: **Sold out** (`available = 0`), **Low stock** (`1 ≤ available ≤ lowStockThreshold`), or **In stock** (`available > lowStockThreshold`).
- The admin inventory list filters by these states separately; the dashboard's replenishment card covers **Sold out and Low stock** together. Exact definitions in [domain-model §4.6](domain-model.md#46-stock-classification).
- `lowStockThreshold` is a **store setting**, default **5**. *[DEFAULT]* Per-variant thresholds are deferred.

---

## 9. Cart

### 9.1 Ownership and identity
1. Every cart belongs to **exactly one store** and to either **one customer** or **one anonymous guest session**.
2. A store has at most one active cart per customer.
3. Carts are **server-side** for both guests and customers.

### 9.2 Guest cart identification
- A guest cart is created on the **first add-to-cart** (not on first visit), so browsing bots do not create carts.
- The server issues an **opaque random token** (≥128 bits from a cryptographically secure generator). The server stores only a **hash** of the token.
- The token is sent in an **`HttpOnly`, `Secure`, `SameSite=Lax`, host-only cookie** (no `Domain` attribute), so a cookie for one store's host is never sent to another store's host.
- The cart is bound to its store; a token presented on another store is ignored and treated as "no cart".

### 9.3 Lifetime
- Guest carts expire **30 days after last activity** (sliding). *[DEFAULT]*
- Customer carts expire **90 days after last activity**. *[DEFAULT]*
- **Activity means a successful change** (add, set quantity, remove, adjust to available, merge, reassignment). Viewing the cart or requesting a checkout quote does **not** extend expiry. An expired cart is treated as non-existent.
- Expired carts are deleted by a backend cleanup job.
- Converted and merged carts are deleted after **7 days**. Orders never depend on cart data.
- When a cart becomes an order, it is closed; the next add-to-cart creates a new cart (and, for guests, a new token).

### 9.4 Cart lines
- A line stores **variant and quantity only**. It may also store the price seen when added, **only** to display a "price changed" notice.
- One line per variant; adding an existing variant increases that line's quantity.
- **Add-to-cart is idempotent on the server:** each add carries a client-generated request id; a retried or replayed request never increments the quantity twice, while two intentional adds (different ids) both apply. Disabling the button in the UI is an additional safeguard only.
- **Maximum quantity per line: 10.** This value is provided by the backend as store configuration; the frontend **never hardcodes it**.
- Maximum 50 lines per cart. *[DEFAULT]*

### 9.5 Validation
- **On add/update:** the variant must belong to the cart's store, its product must be `Published`, the variant must be active, and the requested quantity must be within limits and `≤ available`.
- **On every read:** the backend re-prices the cart and flags lines whose variant became unavailable, archived, unpublished, or under-stocked.
- **No stock is reserved by a cart.** The authoritative stock check happens atomically at order placement (Section 8).
- Lines flagged unavailable are **kept and shown with a reason and an action**, **excluded from totals**, and **block checkout** until the shopper removes or corrects them. Lines are never removed silently.
- Cart and checkout values come from a single backend **quote** calculation, revalidated by **fingerprint** without expiry or price holding ([domain-model §6.6](domain-model.md#66-the-quote)).

### 9.6 Merge on login
Performed by the backend, idempotent. **Login never fails because of a merge**: the merge runs after login and is retried safely if it fails.
1. If the customer has **no** active cart, the guest cart is **reassigned** to the customer.
2. Otherwise the guest cart is **merged** into the customer cart:
   - Same variant in both → keep the **higher** quantity (not the sum).
   - The guest cart is closed.
3. The resulting cart is **fully revalidated** (Section 9.5): store ownership, publication and variant status, current prices, quantity limits, line limit, and `available` stock. Quantities are capped; invalid lines are flagged. The shopper is shown a notice listing any adjustments.
4. The guest cart cookie is cleared after a successful merge or reassignment.
5. Merging an already-merged guest cart is a no-op.

### 9.7 Logout
The customer's cart remains server-side; the browser's guest cookie is cleared so the next person on a shared device does not see it.

---

## 10. Checkout and order placement

### 10.1 Checkout input
| Field | Rule |
|---|---|
| Contact name | Required |
| Contact phone | Required; valid Egyptian mobile number (Section 16.4) |
| Contact email | Optional for guests; for registered customers, taken from the profile (not editable at checkout) |

**Registered customers:** contact name and phone are **prefilled from the profile and editable for this order only**; edits never change the profile. Recipient details and the address are prefilled from the latest order and editable. Everything is stored as an order snapshot ([domain-model §5.7](domain-model.md#57-logged-in-checkout)).
| Delivery address | Required; structured (Section 15) |
| Payment method | `COD` (only option in the MVP) |
| Quote fingerprint | Fingerprint of the final order summary the shopper reviewed |
| Idempotency key | Generated per checkout attempt |

### 10.2 Placement rules
1. All lines must pass cart validation (Section 9.5).
2. Stock is committed for all lines **atomically** — all-or-nothing.
3. The checkout page shows a **final order summary** from the backend. At placement, the backend revalidates prices, stock, shipping rules, and totals; if the result differs from the reviewed summary (fingerprint mismatch), placement is **rejected** and the new summary is shown for review. No silent price changes. The fingerprint detects differences; it does not by itself guarantee validity.
4. On success: order status `Pending`, payment status `Pending`, cart closed, confirmation email sent if an email exists (Section 19).

### 10.3 Order snapshot
At placement, the order stores copies of:
- Contact name, phone, email (if any).
- Full delivery address.
- Per line: product name and option labels (in the order's locale **and** the store's default locale), SKU, unit price, quantity, line total.
- **Shipping rule** (flat fee and free-shipping threshold) and the resulting shipping fee.
- Subtotal, shipping fee, grand total, currency. *(Prices include tax; tax is **not itemized** in the MVP — Section 14.)*
- The locale the order was placed in.

The order references the customer (if registered) and each line references its variant, but **displays only snapshot data**. Later changes to products, prices, settings, or the customer profile never alter an existing order.

### 10.4 Historical accuracy
1. Order history — for customers, guests, and staff — is rendered **only from order snapshots**, never from current catalog, settings, or profile data.
2. This holds after any catalog change: renaming, re-pricing, changing option values, archiving variants, products, or categories.
3. Snapshot fields are **immutable**. The only way order lines change is an order edit (Section 12), which snapshots new lines at edit time and logs before/after values.
4. The line's variant reference is kept for operational use (stock, reporting) but is never needed to display the order.
5. Each order line snapshot references its product's primary image at the time of placement. Media referenced by order snapshots is never deleted ([domain-model §3.4](domain-model.md#34-cleanup-policy)). The text snapshot remains authoritative.

### 10.5 Order number
- Human-readable, **sequential per store**, starting at **1001**, shown as `#1001`. *[DEFAULT]*
- The order number is a **display identifier only**. It is predictable and therefore never sufficient on its own to access an order (Section 17).
- Order numbers are **unique per store**; **gaps are acceptable** (e.g., after a rolled-back placement). Uniqueness and idempotent placement take priority over gap-free numbering.

---

## 11. Order status and payment status

Order (fulfillment) status and payment status are **independent fields** with their own transitions.

### 11.1 Order status

**States:** `Pending`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `DeliveryFailed`.
**Terminal states:** `Delivered`, `Cancelled`, `DeliveryFailed`.

| From | To | Actor | Preconditions | Side effects |
|---|---|---|---|---|
| — | `Pending` | Shopper (checkout) | Section 10.2 | Commit stock; payment `Pending` |
| `Pending` | `Confirmed` | Staff | — | — |
| `Pending` | `Cancelled` | Customer, guest (secret link), or staff | Reason required | Release committed stock; payment → `Voided` |
| `Confirmed` | `Processing` | Staff | — | Order items locked from editing |
| `Confirmed` | `Cancelled` | Staff | Reason required | Release committed stock; payment → `Voided` |
| `Processing` | `Shipped` | Staff | Optional courier name and tracking number | Deduct `onHand` and `committed` |
| `Processing` | `Cancelled` | Staff | Reason required | Release committed stock; payment → `Voided` |
| `Shipped` | `Delivered` | Staff | — | — |
| `Shipped` | `DeliveryFailed` | Staff | Reason required | Payment → `Voided`; stock unchanged until return recorded |

**Rules**
- Any transition not in this table is **rejected** (no skipping, e.g., `Pending → Shipped`; no going backwards).
- Customers may cancel **only while `Pending`**. From `Confirmed` onwards they must contact the store.
- Staff may cancel from `Pending`, `Confirmed`, or `Processing`. Once `Shipped`, the order cannot be cancelled — it ends as `Delivered` or `DeliveryFailed`.
- **Failed delivery attempt vs. failed delivery:**
  - A **failed attempt** (customer not home, phone unanswered) is **not a status**. The order stays `Shipped`, remains eligible for further attempts, and stock and payment are unchanged. Staff record an attempt, if they wish, as an **internal note** ([OQ4](#26-open-questions), resolved).
  - **`DeliveryFailed`** is the **terminal** outcome, set only when the store abandons delivery. It cannot be retried or reversed; a customer who still wants the goods places a new order.
- **Stock across fulfillment:** committed stock is released only by cancellation (before `Shipped`). At `Shipped`, `onHand` and `committed` are both reduced (goods leave the warehouse). Neither failed attempts nor `DeliveryFailed` change stock. Stock is **restored** only when staff **record the return** (Section 8.6), per line, for the units actually restocked.
- **Cancellation reasons** *(revised in v1.9 — approved 2026-10-18)*:
  - *Customer or guest:* changed my mind, ordered by mistake, other.
  - *Staff:* customer request, unreachable for confirmation, out of stock, duplicate order, other.
  - A **note is required** when the reason is "other" (1–500 characters *[DEFAULT]*) and optional otherwise. The reason must come from the list for the actor's type; the backend rejects any other value.
- **DeliveryFailed reasons:** customer refused, customer unreachable, wrong address, other (note required, as above).
- There is no "undo" in the MVP; the admin UI asks for confirmation before every transition.
- **Old `Pending` orders are never cancelled automatically.** The admin has a "Pending for more than 48 hours" filter; their stock stays committed until the order is cancelled or transitions.
- **Courier details:** optional courier name and tracking number on `Shipped`, shown to customers on the order page and guest lookup; staff may correct them later (audited).
- **Delivery-detail edits:** staff may edit recipient name and phone, delivery address, and delivery notes while the order is `Pending`, `Confirmed`, or `Processing`. Before/after values and the staff user are logged; contact details never change; shipping is not recalculated under the flat-fee rule ([domain-model §7.5](domain-model.md#75-fulfillment-details)).

### 11.2 Payment status (COD)

**States:** `Pending`, `Paid`, `Voided`.

| From | To | Actor | Preconditions |
|---|---|---|---|
| `Pending` | `Paid` | Staff ("Mark as paid") | Order status is `Delivered` |
| `Pending` | `Voided` | System | Order moves to `Cancelled` or `DeliveryFailed` |

**Rules**
- `Payment.status` is the source of truth; the order's payment status is a copy updated in the same transaction.
- "Mark as paid" records a **collection date** (`collectedAt` — a business date defaulting to today, not before the delivery date, not in the future) plus the audit timestamp and staff user. The amount equals the order's grand total. Partial payments are deferred.
- There is no ordinary undo. Mistakes are fixed through an **audited payment correction procedure** (revert to `Pending` or change the collection date) requiring a separate permission, password re-entry, and a reason ([domain-model §7.8.2](domain-model.md#782-payment-correction-procedure)).
- `Voided` is terminal; refunds are deferred.
- `Delivered` + payment `Pending` means **delivered, cash not yet collected or remitted**. Delivery never sets payment to `Paid` automatically.
- **Invariants:** `Paid ⇒ Delivered`; `Cancelled` or `DeliveryFailed ⇒ Voided`.

### 11.3 Order event log
Every significant order change is recorded in an append-only **order event log**: placement, status changes, item edits (with before/after), cancellations, payment changes, return recording, and internal notes. Each event records type, actor (staff user, customer, guest, or system), timestamp, details, and optional reason.

Staff identities and internal notes are **never** exposed to shoppers.

### 11.4 Atomicity and concurrency
Applies to: order placement, status transitions, order edits, cancellations, payment changes, return recording, and stock adjustments.

1. **One operation = one database transaction.** Within it, the backend:
   1. checks preconditions (status, payment status, versions, stock);
   2. updates the order (status, lines, totals);
   3. updates inventory (`onHand`, `committed`);
   4. appends the order event(s) and stock movement(s).

   Either **all** of these are committed or **none** are.
2. **Orders use optimistic concurrency.** Every order has a version. Edits and transitions include the version the actor saw; if the order changed in the meantime, the request is rejected (409) and the UI reloads the current state.
3. **Status transitions are compare-and-set** on the expected "from" status (Section 8.4).
4. **Inventory updates are conditional.** Committing stock succeeds only if `committed + qty ≤ onHand` at write time; concurrent checkouts for the last unit cannot both succeed.
5. **Multi-variant operations** (placement, edits) lock or update variants in a consistent order to avoid deadlocks — a backend implementation concern.
6. **Retries are safe:** placement uses an idempotency key; other operations are protected by version and status checks.
7. Correctness of these rules is the backend's responsibility and is verified by backend integration tests against a **real PostgreSQL database** (RA-8); frontend test doubles never substitute for them.

---

## 12. Order editing

1. Staff may edit order items only when order status is `Pending` or `Confirmed` **and** payment status is `Pending`.
2. Allowed edits: change a line's quantity, change a line's variant, add a line, remove a line. An order must keep at least one line — to remove everything, cancel the order.
3. **Pricing on edit**
   - **Untouched lines** keep their snapshot unit price and names.
   - A **quantity change** on an existing line keeps that line's snapshot unit price.
   - A **variant change or new line** uses the variant's **current** effective price, and the line's names are snapshotted at the time of the edit.
4. **Stock on edit:** only the difference is committed or released (Section 8.3). An increase needs only the additional units to be available.
5. **Shipping on edit:** recalculated using the **shipping rule stored on the order**, not the store's current settings. An edit may therefore add or remove free shipping.
6. **Totals:** subtotal, shipping, and grand total are recomputed by the backend (tax is included in prices and not itemized — Section 14).
7. **Preview, then apply.**
   1. *Preview:* the admin's proposed changes are sent to the backend, which validates them and returns the recalculated lines, totals, and a **preview fingerprint** **without saving anything**.
   2. *Apply:* the admin confirms; the request includes the **order version** and the **preview fingerprint** (a hash of the previewed lines, unit prices, quantities, shipping, and totals). The backend recomputes the preview inside the apply transaction; if the order version or the fingerprint differs, the apply is rejected and a new preview is shown. A matching grand total alone is never sufficient, because offsetting changes can keep the total equal while the order differs. *(Revised in v1.9 — approved 2026-10-18.)*
8. Applying an edit is a single atomic operation (Section 11.4): stock changes, new totals, and the order event (with before/after values and the staff user) are saved together or not at all.
9. Existing snapshot data of untouched lines is never modified (Section 10.4).

---

## 13. Shipping

1. Each store configures:
   - **Flat shipping fee** (amount ≥ 0).
   - **Free-shipping threshold** (optional). If set, shipping is free when `subtotal ≥ threshold`. If not set, shipping is never free.
2. The subtotal compared to the threshold is the sum of line totals, before shipping.
3. Staff can update these settings at any time. Changes affect **new orders only**.
4. The order stores the **rule used** (fee and threshold) and the resulting fee (Section 10.3).
5. No courier integration; brands handle delivery manually.

---

## 14. Pricing, money, and tax

### Money
- All amounts are stored as **integer minor units** with a **currency code** (e.g., `{ amount: 15000, currency: "EGP" }` = 150.00 EGP). No floating-point money.
- Each store has one **base currency**; EGP is the only supported currency in the MVP.
- Amounts in different currencies are never added together.

### Tax
*(Revised in v1.9 — approved 2026-10-18.)*
- **Prices are tax-inclusive.** Staff enter prices including tax; storefront, cart, checkout, orders, and emails display prices including tax. Store setting `pricesIncludeTax = true` for all MVP stores, **read-only**; there is no per-store choice between tax-inclusive and tax-exclusive pricing.
- `pricesIncludeTax` states the **pricing policy only**. It does **not** make a tax calculation possible: extracting a tax amount would require a defined rate and applicable rules, which the MVP does not have.
- **The MVP does not calculate, extract, store, or itemize a tax amount.** No tax rate exists anywhere in the system (backend or frontend). Cart, checkout, order pages, and emails show a fixed statement that prices include tax.
- **Future tax engine:** itemized tax is a future, backend-owned capability added without changing existing data — for example tax rules per store or category, with per-line rate and amount snapshots on orders created after it exists. MVP orders simply have no tax snapshot, meaning "not itemized".
- **Launch readiness:** applicable tax and invoicing requirements must be reviewed before a commercial launch ([OQ3](#26-open-questions)).

### Responsibilities
| Concern | Owner |
|---|---|
| Product and variant prices | Catalog (backend), edited by staff |
| Effective price resolution | Backend |
| Cart and order totals, shipping | Backend |
| Displaying and formatting money | Frontend |

The frontend **displays** money; the backend **calculates** money. Any total computed on the frontend is a non-authoritative UI estimate.

---

## 15. Addresses

### Structure
| Field | Required | Notes |
|---|---|---|
| Recipient name | Yes | |
| Recipient phone | Yes | Egyptian mobile; may differ from the contact phone |
| Governorate | Yes | Chosen from a **fixed platform list** of Egyptian governorates (with Arabic and English names) |
| City / area | Yes | Free text in the MVP |
| Street | Yes | |
| Building | Yes | Number or name |
| Floor | No | |
| Apartment | No | |
| Landmark | No | e.g., "next to the pharmacy" |
| Delivery notes | No | Instructions for the courier |

- Country is implicitly Egypt in the MVP; no postal code.
- The governorate list is platform reference data, enabling per-governorate shipping later.

### Address book
- A saved address book is **deferred**.
- For registered customers, checkout is **prefilled from their most recent order's address**. The prefilled address is fully **editable**, and the customer reviews it before placing the order.
- Every order stores a full address snapshot.

---

## 16. Customer accounts and authentication

### 16.1 Accounts
- A customer account belongs to **exactly one store**. The same person at two stores has two independent accounts.
- Registration requires: name, phone, **email**, password.
- **Phone** and **email** are each **unique per store**.
- Profile editing in the MVP: name only. Changing phone or email is deferred. *[DEFAULT]*

### 16.2 Login and sessions
- Customers log in with **phone + password**. Email is used for recovery only, not as a login identifier.
- Sessions are bound to the store; a session for one store is rejected by any other store.
- Login is rate-limited per phone and per IP.
- **Server-side sessions** with opaque, `HttpOnly`, `__Host-` cookies are used for customers and staff (no JWTs). Rotation, expiry, and invalidation rules are in [domain-model §5.5](domain-model.md#55-sessions). Transport details are finalized in Phase 4.

### 16.3 Passwords
- Hashed on the backend with a slow, salted algorithm (Argon2id or bcrypt). Plaintext is **never** stored or logged.
- **No one — including store staff and platform operators — can see a customer's password.** No admin API exposes password data.
- Policy: 8–128 characters; rejected if found in a common-password list; no forced composition rules. *[DEFAULT]*

### 16.4 Phone numbers
- Accepted inputs: local (`01xxxxxxxxx`), international (`+20…`, `0020…`), with spaces or dashes, and Arabic-Indic digits.
- Normalized to a single canonical format (`+201xxxxxxxxx`) before storage and comparison.
- Valid prefixes: `010`, `011`, `012`, `015`.
- Phone numbers are **unverified** in the MVP (no OTP).

### 16.5 Guest orders and accounts
- Guest orders are **never automatically linked** to an account by matching phone or email, because those identifiers are unverified. "Claim past orders" is deferred until verification exists.

### 16.6 Password recovery
The MVP includes the recovery **flow**; actual email delivery is a backend responsibility.

1. Customer submits their email on the forgot-password page.
2. The response is **identical** whether or not an account exists (no account enumeration).
3. If an account exists, the backend generates a **random, single-use token** (≥128 bits), stores only its **hash**, binds it to the store and customer, and sets an expiry of **30 minutes**.
4. The emailed link uses the store's **registered host** from the host mapping — never the incoming request's `Host` header.
5. The reset page uses `Referrer-Policy: no-referrer`; tokens are excluded from analytics and logs.
6. On a successful reset: the token is invalidated, any other outstanding tokens are invalidated, and **all existing sessions are revoked**.
7. Forgot-password requests are rate-limited per email and per IP.

There is **no** manual password reset by store staff.

---

## 17. Guest order access

### 17.1 Status lookup (read-only)
- Input: **order number + phone**, submitted in a request body (never in the URL).
- Both must match an order in the **current store**. On mismatch, the response is a generic "order not found" that does not reveal which input was wrong.
- Rate-limited per IP and per order number; repeated failures are temporarily blocked.

**Shown**
- Order number, placement date.
- Order status and status timeline (status and date only).
- Courier name and tracking number, if recorded.
- Payment status.
- Items (name, option labels, quantity, line total) and totals.
- **Masked delivery location:** governorate and city/area only.
- Recipient **first name** only.

**Never shown**
- Street, building, floor, apartment, landmark, delivery notes.
- Full phone numbers or email.
- Staff identities, internal notes, event log details.
- Any secret link or token.

### 17.2 Cancellation secret link
- Created at placement for **guest** orders and shown on the confirmation page (with a prompt to save it) and in the confirmation email if an email was provided.
- Token: ≥128 bits from a secure random generator, stored **hashed**, bound to the order and store.
- Grants: the same masked view as 17.1 **plus** the ability to cancel (with a reason).
- **Validity:** the link works only while the order is `Pending` **and** for at most **7 days** after placement, whichever ends first. It stops working as soon as the order is cancelled or confirmed.
- An order may have **more than one valid link** (e.g., when a placement response is replayed). Issuing a new link never invalidates an earlier one; all follow the same validity rule.
- The page uses `Referrer-Policy: no-referrer`; tokens are excluded from analytics and logs.
- Once the link is invalid, the guest can still use the read-only lookup or contact the store.

### 17.3 Registered customers
Registered customers view full details and cancel `Pending` orders from their account's order history. No secret link is needed.

---

## 18. Staff accounts and admin access

### 18.1 Staff authentication
- Staff log in with **email + password**, hashed as in 16.3.
- Login is rate-limited per email and per IP.

### 18.2 Authorization
- A staff user can access a store **only** through a **store membership**.
- Admin routes include the store explicitly (`/stores/:storeId/...`).
- On **every** admin API request, the backend verifies that the authenticated staff user has a membership for that store. Requests for a store without membership return **404** (not 403), so the existence of other stores is not revealed.
- One role (store admin) in the MVP.

### 18.3 Initial credentials (seeding)
- **No credentials are committed to source control.** Real `.env` files are git-ignored; a committed `.env.example` contains placeholders only.
- The provisioning script takes the initial admin **email** as input and **generates a random temporary password** with a cryptographically secure generator, printing it **once** to the operator's console. *(v1.9: the option to supply the password through an environment variable or secret store is removed — no requirement needs it, and it would put a plaintext password in configuration. Approved 2026-10-18.)*
- Only the password hash is stored. The account is flagged **must change password on first login**.
- The temporary password is shown **once, only after provisioning succeeds completely**, and is **never logged or persisted** in plaintext.
- **Existing staff email:** the platform-wide identity is **reused** and receives a new store membership with an explicitly assigned role. No duplicate identity is created, the existing password is **not** reset, no temporary password is generated or revealed, and no permissions carry over from other stores ([domain-model §1.15](domain-model.md#115-provisioning-and-existing-staff-identities)).
- **Local development:** development stores and staff users are created with the same provisioning script. No development credentials are committed or shared.

### 18.4 Known limitation: staff password recovery
Self-service staff password recovery is **deferred**. Until it exists, a forgotten staff password can only be reset by the **platform operator** running the **staff password reset command** — separate from provisioning, which never changes an existing credential. The command issues a new random temporary password (shown once), sets "must change password", **revokes all of the staff user's existing sessions**, and records a `StaffPasswordReset` platform audit event. *(Revised in v1.9 — approved 2026-10-18.)*

---

## 19. Notifications

| Notification | Channel | Sent when | MVP |
|---|---|---|---|
| Order confirmation | Email | Order placed **and** an email exists | ✅ |
| Password reset | Email | Customer requests reset | ✅ |
| Password changed | Email | After a successful password change or reset (customers and staff) | ✅ |
| Order status updates (confirmed, shipped, …) | Email | — | Deferred |
| SMS / WhatsApp | — | — | Deferred |

- Emails are sent by the backend; the frontend has no email responsibility.
- **Delivery is at least once** through a **transactional outbox**: a message is recorded in the same transaction as its business change, so it exists only if that change committed. Retries re-send the message only and **never repeat the business operation**. Rare duplicate emails are acceptable in the MVP ([system-behavior §5.4](system-behavior.md#54-notification-delivery-is-separate-from-business-idempotency)).
- Emails display the store's name and logo and are sent from a platform address. Store-specific sender domains are deferred.
- The order confirmation email includes: order number, items, totals, masked delivery location, and — for guests — the cancellation secret link.
- Guests who provide no email receive confirmation **only on the confirmation page**, which prominently asks them to save their order number and secret link.
- The password-changed email is **informational only**: it states when the change happened and what to do if it was not the recipient. It **never** contains the password or any token.

---

## 20. Admin dashboard

### 20.1 Conventions
- Timezone: **Africa/Cairo** for all date ranges and displays; timestamps stored in UTC.
- Default range: **last 30 days**, selectable.
- All monetary metrics are in the store's base currency.

### 20.2 Metric definitions

| Metric | Rule | Date basis |
|---|---|---|
| **Total orders** | Count of orders in **any** status | `placedAt` in range |
| **Pending orders** | Count of orders **currently** `Pending` (a work queue, not a period metric) | Current state |
| **Cancelled orders** | Count of orders in status `Cancelled` | `placedAt` in range |
| **Delivered & paid sales** | Sum of `grandTotal` (including shipping, after edits) over orders with status `Delivered` **and** payment `Paid`. Computed live; payment corrections restate past periods ([domain-model §7.8.3](domain-model.md#783-sales-reporting)). | `collectedAt` (business date) in range |
| **Needs replenishment** | Count of **distinct** non-archived products with ≥1 active variant that is **Sold out or Low stock** (Section 8.8); the detail list shows the variants and their state | Current state |
| **Recent orders** | The 10 most recent orders | By `placedAt` |

### 20.3 No double counting
- Sales are summed **per order**, never per payment record.
- Each order contributes **at most once** to each metric.
- An edited order contributes its **current** totals once.

---

## 21. Admin capabilities

| Area | MVP capabilities |
|---|---|
| **Overview** | Metrics (Section 20), recent orders |
| **Products** | List, search (name/SKU), filter by status; create, edit, publish/unpublish, archive/restore; translations; images; options and variants; base and variant prices; stock per variant |
| **Categories** | List, create, edit, archive/restore; translations; two levels |
| **Orders** | List, search (order number, customer name, phone), filter by order/payment status and by "Pending > 48 h", "Delivered, cash not collected", "Failed delivery, return not recorded"; details (contact, address, items, totals, courier details, event log); status transitions; item editing; delivery-detail editing; cancellation with reason; mark as paid; payment correction; record return; internal notes |
| **Inventory** | Stock per variant (`onHand`, `committed`, `available`); adjustments with reason; low-stock list; movement history |
| **Store settings** | Store name, logo, basic theme (colors, typography — details in Phase 5), supported and default locales, base currency (EGP), flat shipping fee, free-shipping threshold, low-stock threshold |

Admin screens are designed after the domain model is approved.

---

## 22. Security requirements

1. **Backend-enforced isolation.** Every query and write is scoped to the resolved store. Frontend checks are UX only.
2. **Cross-store reference validation** on every write (Section 5.5).
3. **Non-enumerable identifiers.** Internal IDs are random (e.g., UUIDs). Order numbers are display-only.
4. **Secrets and tokens** (cart, reset, order access) are ≥128 bits, generated by a secure random source, stored **hashed**, scoped to a store, and expire.
5. **Passwords** hashed with Argon2id or bcrypt; never stored, logged, or displayed in plaintext.
6. **Cookies:** `HttpOnly`, `Secure`, host-only, `__Host-` prefixed; `SameSite=Lax` on the storefront and `SameSite=Strict` on the admin.
7. **No personal data in URLs or logs** (phone numbers, emails, tokens).
8. **Rate limiting** on login, forgot-password, guest order lookup, checkout, and cart writes.
9. **No account enumeration** in recovery or lookup responses.
10. **Links in emails** use the store's registered host, never the request's `Host` header.
11. **HTTPS everywhere.**
12. **Seeded credentials** never in source control (Section 18.3).
13. **Development tooling** (seed data, local proxies, test doubles, test-only diagnostics) is never deployed and provides no security guarantees.
14. **CSRF protection** for all cookie-authenticated requests (Section 22.1).
15. **Platform-operator audit:** sensitive operator actions (provisioning, activation, suspension, staff identity and membership changes, staff password resets) are recorded as append-only **PlatformAuditEvents** with actor, action, target, timestamp, **outcome**, and non-sensitive context — refused and failed commands included. The actor is a **declared** operator name or ID, not authenticated proof of identity and not a substitute for authentication or authorization; there are no operator accounts in the MVP. Audit records never contain credentials, secrets, or tokens ([domain-model §1.14](domain-model.md#114-platformauditevent)). *(Outcome and declared-identity wording: RA-1 second review, 2026-10-18.)*
16. **Locking and suspension enforcement:** **no operation prohibited while a store is suspended may commit after the suspension commits** — there is no exception for races. Order placement, order item-edit apply, and customer/guest cancellation take **shared** locks on the organization and store rows; store/organization status changes take **exclusive** locks. The other prohibited writes — cart writes (including the login merge), customer registration, and staff catalog, media, settings, and branding writes — are synchronized with status changes as well; their mechanism is decided in the architecture phase ([system-behavior §7.4](system-behavior.md#74-items-deferred-to-the-architecture-phase), P4-1) without serializing non-conflicting operations against each other. All other operations use narrower locks in a fixed global order ([system-behavior §5](system-behavior.md#5-concurrency-locking-and-idempotency)). **Decision D8 is preserved:** fulfillment of existing orders and the staff operations D8 permits remain available while suspended, subject to permissions, workflow rules, stock invariants, and audit logging. *(Revised in v1.9 — RA-6, approved 2026-10-18 with this narrow interpretation; extended to all prohibited writes in the second review, 2026-10-18 — **pending approval**.)*
17. **Trusted host:** the store is identified only from the host received by the trusted edge; client-supplied forwarded-host headers are never trusted. Edge-to-SSR, edge-to-API, and SSR-to-API credentials are separate, and the SSR server's credential is limited to public read endpoints. *(Requirement approved 2026-10-18; design in [ADR-04](architecture-decisions.md#adr-04-request-topology-host-resolution-and-public-caching) revision 4 — approved 2026-10-18.)*

### 22.1 CSRF protection
Guest cart sessions, customer sessions, and staff sessions all use cookies, which the browser attaches automatically — so every state-changing request must prove it came from our own app.

1. **No state changes via `GET`.** Only `POST`, `PUT`, `PATCH`, `DELETE` change state, and they accept only `application/json` bodies.
2. **`SameSite=Lax` is necessary but not sufficient.** "Same-site" is based on the registrable domain, so `brand-a.<platform-domain>` and `brand-b.<platform-domain>` are **same-site** with each other. SameSite alone does not isolate stores on platform subdomains.
3. **CSRF token on every state-changing request:**
   - The backend issues a CSRF token **bound to the session** (e.g., signed with the session identifier), so a token from another session or store is rejected.
   - The token is delivered in a readable cookie and sent back by the app in a request header. Angular's built-in `HttpClient` XSRF support provides the client side.
   - The backend rejects state-changing requests whose header token is missing or does not match the session.
4. **Origin check:** state-changing requests must carry an `Origin` (or `Referer`) header matching the store's registered host (storefront) or the admin host (admin).
5. **Cookie hardening:** session, cart, and CSRF cookies use the `__Host-` prefix (requires `Secure`, `Path=/`, no `Domain`), preventing a sibling subdomain from setting or overwriting them.
6. **Login and registration are protected too** (pre-session CSRF token), preventing login CSRF.
7. Concrete mechanism and token lifecycle are finalized in Phase 4.

---

## 23. Non-functional requirements

| Category | Requirement |
|---|---|
| **Performance** | Mobile-first. Target "good" Core Web Vitals on a mid-range Android phone over 4G for storefront public pages. |
| **SEO and sharing** | Public storefront pages are server-rendered with per-store, per-locale titles, descriptions, and Open Graph tags (product image, name, price) so links preview correctly on WhatsApp, Facebook, and Instagram. |
| **Accessibility** | WCAG 2.1 AA basics: keyboard navigation, labels, focus states, sufficient contrast — including with brand theme colors. |
| **Internationalization** | Full RTL/LTR support from day one (Section 6). |
| **Browser support** | Last 2 versions of Chrome, Edge, Firefox, Safari; Samsung Internet. |
| **Scale (MVP)** | Tens of stores; up to a few thousand products per store; up to a few hundred orders per day per store. All lists paginated. |
| **Time** | Stored in UTC; displayed in Africa/Cairo. |
| **Auditability** | Order changes and stock changes are fully logged (Sections 8.7, 11.3). |
| **Testability** | Business rules in this document — especially inventory, transitions, pricing, and isolation — must be covered by automated tests. |
| **Caching** | Public catalog pages and APIs may be up to **60 seconds** stale (time-based expiry only; no on-demand invalidation in the MVP); they contain no user-specific content, never set cookies, and are keyed by store and locale. Cart, checkout, account, order, auth, and admin responses are never publicly cached. The suspended-store `503` page is not cached. Cached data is never authoritative for a transaction: business operations always check the **current** store status in the database. Details in [system-behavior §4](system-behavior.md#4-caching). |
| **Reconciliation** | A daily (and manually triggerable) **read-only** job checks stock, payment, and order-total invariants, logs mismatches with diagnostic context, and alerts the platform operator. It never corrects data and does not replace transactional safeguards. Details in [system-behavior §3](system-behavior.md#3-reconciliation). |

---

## 24. Provisional architecture decisions

Recorded here because they constrain the requirements. They are re-examined in Phases 4 and 5.

| Decision | Summary |
|---|---|
| **Two apps, one workspace** | One Angular workspace with two applications (`storefront`, `admin`) and shared libraries (design system, i18n/formatting, API contract types). |
| **Storefront rendering** | Angular SSR with hybrid rendering: public pages (home, category, product) server-rendered; cart, checkout, and account client-rendered. |
| **Admin rendering** | Client-side only. |
| **Storefront tenant resolution** | Host name → store lookup by the backend. Subdomains in the MVP; custom domains later. Path-based tenancy rejected. |
| **Admin hosting** | Separate admin host (e.g., `admin.<platform-domain>`); store in the route. |
| **Same-origin API** | Each app calls its API under the same origin (`/api`), simplifying cookies and avoiding CORS. Dev uses a proxy. |
| **Development approach** *(v1.9, replaces "Mock API" — approved 2026-10-18)* | Features are built as end-to-end slices against the real backend. Frontend test doubles may be used for isolated component and unit tests, but there is **no separate mock API implementation**. The API's OpenAPI document, generated from the backend, is the contract; Angular's contract types are generated from it ([ADR-03](architecture-decisions.md#adr-03-repository-layout-and-api-contracts)). |
| **Backend** *(approved 2026-10-18 — ADR-01/02)* | C# on .NET 10 (LTS) with ASP.NET Core; PostgreSQL with EF Core. Details in [architecture-decisions.md](architecture-decisions.md). |
| **State management** | No NgRx or facade layer assumed; evaluated in Phase 4. |

---

## 25. Assumptions

### 25.1 Confirmed (now requirements)

| # | Statement |
|---|---|
| A1 | Each store owns its own catalog, prices, categories, inventory, customers, and orders. Cross-store catalog sharing is deferred. |
| A2 | Displayed prices include tax; tax rules owned by the backend. |
| A3 | Customer accounts are store-scoped. |
| A4 | Phone numbers are unverified; no automatic linking of guest orders. |
| A5 | Email required for registered accounts; optional for guests. |
| A6 | The admin dashboard uses a neutral platform UI, independent of any brand theme. |
| A7 | Stock is committed at placement and released on cancellation (Section 8). |
| A8 | One admin role per store; staff created by provisioning only. |
| A9 | Every product has ≥1 variant; simple products have one default variant. |
| A10 | Products, variants, and categories are archived, never hard-deleted. |

### 25.2 Remaining assumptions
Not explicitly confirmed. They are believed safe; challenge any that are wrong.

| # | Assumption | Affects |
|---|---|---|
| R1 | Phone numbers (contact and recipient) are **Egyptian mobile numbers** only; no landlines or foreign numbers. | Validation, future markets |
| R2 | Every MVP store operates in Egypt, in the **Africa/Cairo** timezone. | Dashboard ranges, timestamps |
| R3 | A store's base currency **cannot change** once it has orders. | Store settings |
| R4 | Each store's storefront is served on a **platform subdomain** in the MVP. | Tenant resolution, deployment |
| R5 | Brands deliver orders with their own couriers and mark cash as collected manually ("Mark as paid"). | Order and payment workflow |
| R6 | Order numbers are unique **per store**, not platform-wide. | Order model, admin search |
| R7 | Confirmation emails are sent in the locale the order was placed in. | Notifications |
| R8 | Email delivery is a backend concern; the provider is not chosen yet. | Notifications, recovery |
| R9 | All values marked **[DEFAULT]** in this document. | Various |

R10 (whole-unit quantities only; no products sold by weight or length) was **confirmed** on 2026-10-18.

Cross-domain assumptions **RA-1 to RA-8** (operator identity, provisioning, locale URLs, idempotency, locking, caching, database primitives) are listed with their full text in [system-behavior §7.2](system-behavior.md#72-assumptions-ra-1-to-ra-8). All eight were **approved on 2026-10-18** (RA-6 with a narrow interpretation that preserves D8) and **re-reviewed the same day**: RA-1, RA-2, RA-3, RA-4, RA-7, and RA-8 are approved with clarifications; **RA-5 and RA-6 were revised and are pending approval**. Implementation details deferred to the architecture phase are listed in [system-behavior §7.4](system-behavior.md#74-items-deferred-to-the-architecture-phase).

---

## 26. Open questions

No open question blocks Phase 2.

| # | Question | Resolve by |
|---|---|---|
| OQ1 | Theme customization scope (which tokens a store may change) | Phase 5 |
| OQ2 | Customer data deletion/export and compliance with Egypt's Personal Data Protection Law (Law 151 of 2020) | Before a real launch |
| OQ3 | **Tax and invoicing:** which tax, invoice, and e-receipt obligations apply to stores selling through the platform, and whether itemized tax is required on any document the platform issues. The MVP issues no tax invoices or receipts and itemizes no tax (Section 14). **Commercial-launch gate:** must be resolved before a commercial launch; resolving it does **not** add tax calculation to the MVP. *(Added in v1.9; U-6 approved 2026-10-18.)* | Before a commercial launch |
| ~~OQ4~~ | **Resolved 2026-10-18 (U-1):** staff record a failed delivery attempt as an **internal note** (`NoteAdded`) while the order stays `Shipped`; no new status or event type in the MVP. | — |

---

## 27. Deferred features

| Area | Deferred |
|---|---|
| **Platform** | Super-admin UI; multiple stores per organization; catalog sharing across stores; custom domain onboarding; billing/subscriptions |
| **Staff** | Granular roles; staff invitations; self-service staff password recovery |
| **Customers** | OTP/phone verification; claiming guest orders; changing phone/email; saved address book; account deletion and data export; account creation during checkout |
| **Catalog** | Multiple categories per product; category depth > 2; compare-at (strikethrough) prices; per-variant low-stock thresholds; full-text search, filters, facets; product duplication; **changing locked slugs and old-slug redirects**; Arabic slug transliteration; rich-text/HTML descriptions; color swatches |
| **Media** | Media library browsing; storage quotas; image editing; video; access-controlled media |
| **Orders** | Undo or backward transitions; customer-requested returns; refunds; partial payments |
| **Inventory** | Forcing `committed` down outside order operations; multiple warehouses; stock reservations at add-to-cart; fractional quantities and products sold by weight or length; bulk stock import |
| **Payments** | Online payments (cards, wallets, Fawry, Paymob, etc.) |
| **Pricing** | Coupons and discounts; multi-currency |
| **Shipping** | Governorate-based fees; courier integrations |
| **Engagement** | Wishlist; reviews; abandoned-cart recovery |
| **Notifications** | Status-update emails; SMS; WhatsApp; store-specific email sender domains |
| **Analytics** | Advanced reporting |

---

## 28. Decision log

| Date | Decision |
|---|---|
| 2026-10-13 | Shared SaaS; one codebase and one running system for all brands |
| 2026-10-13 | Organization → Store model; Store is the isolation boundary; one store per organization in the MVP |
| 2026-10-13 | Egypt first: Arabic/English, RTL/LTR, EGP, cash on delivery |
| 2026-10-13 | MVP includes storefront and brand admin; super-admin UI deferred |
| 2026-10-13 | Guest checkout; phone required; email required only for registered accounts |
| 2026-10-13 | Customer login: phone + password; staff login: email + password |
| 2026-10-13 | Flat shipping fee + optional free-shipping threshold, snapshotted on orders |
| 2026-10-13 | Per-field locale fallback to the store's default locale |
| 2026-10-13 | Prices include tax; all money logic on the backend |
| 2026-10-13 | Order status and payment status are separate; `DeliveryFailed` included |
| 2026-10-13 | Inventory model `onHand` / `committed` / `available` |
| 2026-10-13 | Anonymous server-side guest cart with merge on login |
| 2026-10-13 | Guest order access: number + phone (read-only, masked) and a secret cancellation link |
| 2026-10-13 | Up to 3 option dimensions; variant price inherits base price; one primary category; two category levels |
| 2026-10-13 | Max 10 units per cart line, provided as backend configuration |
| 2026-10-13 | Compare-at price and coupons deferred |
| 2026-10-13 | Provisional: two Angular apps in one workspace; SSR storefront; subdomain tenant resolution |
| 2026-10-13 | Approved C3: order edits keep original prices for untouched lines and quantity changes; preview-then-apply; atomic |
| 2026-10-13 | Approved C5: only order-confirmation and password-reset emails in the MVP |
| 2026-10-13 | Approved C6: structured Egyptian addresses; editable prefill from latest order; no address book |
| 2026-10-13 | Approved C7: option dimensions lock after first publication |
| 2026-10-13 | `onHand < committed` adjustments rejected; inventory discrepancy workflow defined |
| 2026-10-13 | A1 and A6 confirmed |
| 2026-10-13 | Order history rendered only from immutable snapshots |
| 2026-10-13 | Order and inventory operations are atomic with optimistic concurrency |
| 2026-10-13 | CSRF protection: session-bound token, Origin check, `__Host-` cookies |
| 2026-10-13 | **Requirements v1.0 approved — Phase 1 complete** |
| 2026-10-18 | Store has a canonical brand name and a localized display name |
| 2026-10-18 | Suspended stores: no new business; fulfillment and read-only customer order access continue |
| 2026-10-18 | Public support contacts: phone required; email and WhatsApp optional |
| 2026-10-18 | Staff: platform-wide identity; store access via memberships; permission-based authorization |
| 2026-10-18 | Latin slugs per store, locked after first publication; slug changes and redirects deferred |
| 2026-10-18 | Sold-out products stay visible; public stock shown as states only |
| 2026-10-18 | Plain-text descriptions |
| 2026-10-18 | Store-scoped Media for product images and logos, with automatic cleanup; order-referenced media retained |
| 2026-10-18 | Catalog and Media domains approved |
| 2026-10-18 | Inventory: Receive/Remove/Count adjustments; initial stock at variant creation; exclusive stock states; whole units only |
| 2026-10-18 | Inventory domain approved |
| 2026-10-18 | Server-side sessions with opaque cookies for customers and staff; no JWTs |
| 2026-10-18 | Password credentials separated from customer and staff identities |
| 2026-10-18 | Logged-in checkout: contact name and phone editable per order |
| 2026-10-18 | Third email type: password changed |
| 2026-10-18 | Customer and Authentication domain approved |
| 2026-10-18 | Unavailable cart lines kept, excluded from totals, block checkout |
| 2026-10-18 | Fingerprint-based quote revalidation; no expiry; no price holding |
| 2026-10-18 | Cart may disclose available quantity for insufficient stock, with "Adjust to available" |
| 2026-10-18 | Converted/merged carts deleted after 7 days; order-number gaps acceptable |
| 2026-10-18 | Cart domain approved |
| 2026-10-18 | Optional courier details on `Shipped`; correctable with audit |
| 2026-10-18 | COD collection recorded with a business date (`collectedAt`) separate from `deliveredAt` and audit fields; sales reported by `collectedAt` |
| 2026-10-18 | Audited payment correction procedure instead of undo |
| 2026-10-18 | Delivery details editable before `Shipped` |
| 2026-10-18 | No automatic cancellation of `Pending` orders; "> 48 h" filter |
| 2026-10-18 | Order, Payment, and Fulfillment domain approved — **core domains complete** |
| 2026-10-18 | Read layer for cross-domain display views; acyclic domain dependencies |
| 2026-10-18 | Daily read-only reconciliation of stock, payment, and totals; operator alerts |
| 2026-10-18 | Public catalog pages may be ≤ 60 s stale; user-specific responses never publicly cached |
| 2026-10-18 | Locale prefix in URLs (`/ar/`, `/en/`) with canonical and `hreflang`; TTL-only cache expiry |
| 2026-10-18 | Server-side idempotency for add-to-cart and inventory Receive/Remove via client request ids |
| 2026-10-18 | PlatformAuditEvent for operator actions; media `unreferencedSince`; no auto-login after reset |
| 2026-10-18 | Provisioning reuses existing staff identities without credential changes |
| 2026-10-18 | Narrowest-scope locking with a global lock order; notifications separate from business idempotency |

**v1.9 changes** (architecture review, 2026-10-18; details in the [change record](proposed-doc-changes.md)).

| Date | Change | Status |
|---|---|---|
| 2026-10-18 | TTL-only cache expiry (≤ 60 s) everywhere; business operations always read current store status (C1) | Approved 2026-10-18 |
| 2026-10-18 | Separate cancellation reasons for customers/guests and staff; note required for "other" (C2) | Approved 2026-10-18 |
| 2026-10-18 | Item-edit apply verified by order version + preview fingerprint, never by total alone (C3) | Approved 2026-10-18 |
| 2026-10-18 | Status and phase labels updated after the changes were applied (C4) | Approved 2026-10-18 |
| 2026-10-18 | Receive/Remove are deltas; Count/Contain use the expected `onHand`; all four idempotent per `(storeId, requestId)`; no inventory `version` (A1, A2, RA-5) | Approved 2026-10-18 |
| 2026-10-18 | Category archive/restore uses `catalog.write` (A3) | Approved 2026-10-18 |
| 2026-10-18 | Temporary staff passwords are always generated; staff password reset is a separate audited command that revokes sessions (A4, A5) | Approved 2026-10-18 |
| 2026-10-18 | No separate mock API; end-to-end slices; OpenAPI-generated contract types (D1) | Approved 2026-10-18 |
| 2026-10-18 | Tax-inclusive prices; no tax amount calculated or itemized in the MVP; tax/invoice compliance is a commercial-launch gate (D2, OQ3) | Approved 2026-10-18 |
| 2026-10-18 | Failed delivery attempts keep the order `Shipped` and are recorded as internal notes; `DeliveryFailed` is terminal; stock restored only by recording the return (R2, OQ4/U-1) | Approved 2026-10-18 |
| 2026-10-18 | Suspended `503` page not cached; responses private unless explicitly public (R3) | Approved 2026-10-18 |
| 2026-10-18 | Inventory idempotency processing order and integration tests IN-1–IN-9 (R4) | Approved 2026-10-18 |
| 2026-10-18 | RA-1–RA-8 approved; RA-6 interpreted narrowly: placement, item-edit apply, and customer/guest cancellation serialize with suspension; **D8 preserved** | Approved 2026-10-18 |
| 2026-10-18 | U-1–U-4 and U-6 approved as recommended (internal note for attempts; error log + email alerts; `Retry-After` 3600 s; 1–500-character note; tax/invoice review before launch) | Approved 2026-10-18 |
| 2026-10-18 | M1-D1–M1-D5 approved as proposed | Approved 2026-10-18 |
| 2026-10-18 | Architecture revision 3 approved **except ADR-04**; ADR-12 deferred | Approved 2026-10-18 |
| 2026-10-18 | Store identified only via the trusted edge host, with separate edge-to-SSR, edge-to-API, and SSR-to-API credentials (R1) | Requirement approved; ADR-04 revision 4 approved 2026-10-18 (see next entry) |
| 2026-10-18 | **ADR-04 revision 4 approved** — §4a–4g, tests TH-1–TH-14; commit `849fe35`, merged to `main` in `c9edca5` | Approved 2026-10-18 |
| 2026-10-18 | U-5 (production edge capability) | **Open** — resolve before the first shared environment |
| 2026-10-18 | **Requirements v1.9 approved** | Approved 2026-10-18 |
| 2026-10-18 | RA second review: RA-1 (declared operator identity; audit outcome incl. refusals), RA-2, RA-3 (deterministic redirects; cache-key components), RA-4 (add-to-cart contract; idempotency from the server record), RA-7 (cache keys include store, locale, path, response-affecting context), RA-8 (PostgreSQL selected; detailed concurrency design in the architecture phase) | Approved with clarifications 2026-10-18 |
| 2026-10-18 | RA-5 revised: Count/Contain request ids justified; conflict responses and audit behavior defined; remaining choices deferred (P4-5, P4-6) | **Pending approval** |
| 2026-10-18 | RA-6 revised: no prohibited operation may commit after a suspension commits; earlier race exception for cart writes, registrations, catalog/settings writes withdrawn; merge skipped while suspended; mechanism deferred (P4-1); **D8 preserved** | **Pending approval** |
