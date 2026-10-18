# White-Label E-commerce Platform — UI Design (Phase 5)

| | |
|---|---|
| **Status** | **Draft — pending your review.** Nothing here is approved. |
| **Last updated** | 2026-10-18 |
| **Phase** | 5 — UI design |
| **Based on** | [requirements.md](requirements.md) v1.9 (approved), [domain-model.md](domain-model.md) (approved), [system-behavior.md](system-behavior.md), [architecture-decisions.md](architecture-decisions.md) |

> **How to use this document.** It defines the design system, theming, and screens for the two MVP applications (`storefront`, `admin`). It must not contradict [requirements.md](requirements.md); where it would, requirements win and this document is corrected. It proposes a resolution for **[OQ1](requirements.md#26-open-questions)** (theme customization scope) in [Section 4](#4-brand-theming-proposed-resolution-of-oq1).
>
> Items marked **[PROPOSED]** are design choices awaiting approval. Everything else restates an approved requirement, with a link.

---

## Table of contents

1. [Design principles](#1-design-principles)
2. [Design system library](#2-design-system-library)
3. [Tokens](#3-tokens)
4. [Brand theming (proposed resolution of OQ1)](#4-brand-theming-proposed-resolution-of-oq1)
5. [Localization and direction](#5-localization-and-direction)
6. [Storefront screens](#6-storefront-screens)
7. [Admin screens](#7-admin-screens)
8. [Shared patterns](#8-shared-patterns)
9. [Accessibility](#9-accessibility)
10. [Performance budget](#10-performance-budget)
11. [Open design questions](#11-open-design-questions)

---

## 1. Design principles

1. **One design, many brands.** Every store renders the same components and layouts; brands differ only by the tokens in [Section 4](#4-brand-theming-proposed-resolution-of-oq1). No brand-specific components ([requirements §1](requirements.md#1-product-overview)).
2. **Mobile first.** Designed at 360 px wide first, then widened. The target is a mid-range Android phone on 4G ([requirements §23](requirements.md#23-non-functional-requirements)).
3. **Bidirectional by construction.** Every component is built and reviewed in both `ar` (RTL) and `en` (LTR); neither is an afterthought.
4. **Honest commerce.** No fake urgency, no hidden costs. Prices, shipping, and the total are visible before the shopper commits; changes are always shown, never silent ([requirements §10.2](requirements.md#102-placement-rules)).
5. **Calm admin.** The admin uses a neutral platform UI independent of any brand ([requirements A6](requirements.md#25-assumptions)), optimized for dense, repeated daily work.

---

## 2. Design system library

The Angular workspace already has a shared **design system** library ([requirements §24](requirements.md#24-provisional-architecture-decisions), [ADR-03](architecture-decisions.md#adr-03-repository-layout-and-api-contracts)). **[PROPOSED]** structure:

| Layer | Contents | Used by |
|---|---|---|
| **Tokens** | CSS custom properties (Section 3); one platform default theme; brand overrides injected at runtime | Both apps |
| **Primitives** | Button, icon button, text field, phone field, select, checkbox, radio, quantity stepper, badge, chip, spinner, skeleton | Both apps |
| **Patterns** | Price, stock state, form section, empty state, error summary, confirmation dialog, toast, timeline | Both apps |
| **Storefront components** | Product card, gallery, option selector, cart line, quote summary, header, footer | `storefront` |
| **Admin components** | Data table, filter bar, status/payment badges, transition dialog, translation tabs, variant grid | `admin` |

**[PROPOSED] Technology choices**
- **Angular CDK** for behavior: `Directionality` (bidi), `a11y` (focus trap, live announcer, focus monitor), `overlay`, `listbox`. It ships no visual styles, so it does not fight brand theming.
- **Own components** on top of the CDK for everything the storefront renders, so they are small, SSR-safe, and fully driven by tokens.
- **Angular Material is allowed in `admin` only** (tables, dialogs, date picker), themed with platform tokens. It is never imported by `storefront`, keeping it out of the public bundle.
- **Storybook** for the library, with toolbar toggles for locale (`ar`/`en`), direction, color scheme, and three sample brand themes. Visual regression snapshots run on the RTL and LTR variants of each story.

---

## 3. Tokens

All values are CSS custom properties on `:root`. Components read **semantic** tokens only, never raw values.

### 3.1 Color — semantic tokens

| Token | Purpose | Brand-overridable |
|---|---|---|
| `--color-brand` | Primary actions, links, selected states | ✅ |
| `--color-on-brand` | Text and icons on `--color-brand` | Computed (Section 4.3) |
| `--color-brand-subtle` | Tinted backgrounds (selected chip, banner) | Computed |
| `--color-accent` | Secondary emphasis (sale ribbon later, highlights) | ✅ |
| `--color-on-accent` | Text on accent | Computed |
| `--color-bg`, `--color-surface`, `--color-surface-raised` | Page, card, popover | ❌ |
| `--color-text`, `--color-text-muted` | Body and supporting text | ❌ |
| `--color-border`, `--color-border-strong` | Hairlines, inputs | ❌ |
| `--color-success`, `--color-warning`, `--color-danger`, `--color-info` (+ `-subtle`, `-on`) | Status | ❌ |
| `--color-focus` | Focus ring | ❌ (must stay visible on every brand) |

Status colors are **never** brand-overridable, so "Sold out", errors, and order statuses mean the same thing in every store.

### 3.2 Typography

| Token | Value |
|---|---|
| `--font-body`, `--font-heading` | From the chosen font pair (Section 4.2) |
| Scale | 12 / 14 / 16 / 18 / 22 / 28 px; body text 16 px (prevents iOS zoom on inputs) |
| Weights | 400 and 600 only (keeps font downloads to two files per script) |
| Line height | 1.5 for Latin, **1.7 for Arabic** (Arabic glyphs need more vertical room) — set per `:lang()` |

### 3.3 Spacing, shape, elevation, motion

| Token | Value |
|---|---|
| Spacing | 4-based scale: 4, 8, 12, 16, 24, 32, 48 px; page gutter 16 px on mobile |
| `--radius-control`, `--radius-card` | From the brand corner style (Section 4.2) |
| Elevation | 0 (flat), 1 (cards on hover, sticky bars), 2 (dialogs, sheets) |
| Motion | 150 ms (feedback), 250 ms (sheets, dialogs); all disabled under `prefers-reduced-motion` |
| Breakpoints | `sm` 600 px, `md` 900 px, `lg` 1200 px; content max width 1280 px |
| Touch targets | Minimum 44 × 44 px |

---

## 4. Brand theming (proposed resolution of OQ1)

**[PROPOSED]** A store may change **exactly** the settings below. Everything else is fixed by the platform. This keeps every brand on one tested design and makes contrast guarantees enforceable.

### 4.1 What a store can change

| Setting | Type | Default |
|---|---|---|
| Logo | Store media asset ([requirements §7](requirements.md#7-catalog)); SVG or PNG; shown at max 40 px height in the header | Store name in text |
| Brand color | Hex color | Platform default (neutral dark teal) |
| Accent color | Hex color, optional | Derived from brand color |
| Corner style | `sharp` (2 px) · `soft` (8 px) · `round` (16 px controls, pill buttons) | `soft` |
| Font pair | One of the approved pairs in 4.2 | Pair 1 |

Not changeable in the MVP: layouts, section order on Home, spacing, font sizes, status colors, focus color, custom CSS. These are listed as deferred in [Section 11](#11-open-design-questions).

### 4.2 Approved font pairs

Each pair covers Arabic and Latin, is free (OFL), and is **self-hosted** with `font-display: swap` and subset files.

| # | Arabic | Latin | Character |
|---|---|---|---|
| 1 | IBM Plex Sans Arabic | IBM Plex Sans | Neutral, highly legible (default) |
| 2 | Cairo | Inter | Modern, friendly |
| 3 | Tajawal | Poppins | Rounded, fashion and lifestyle |
| 4 | Noto Kufi Arabic | Noto Sans | Geometric headings, wide coverage |

The font for each run of text is chosen by `:lang(ar)` / `:lang(en)`, so mixed-language content ([requirements §6 API behavior](requirements.md#6-localization)) renders each part in the right font.

### 4.3 Contrast enforcement

[Requirements §23](requirements.md#23-non-functional-requirements) requires WCAG AA contrast **including with brand theme colors**. **[PROPOSED]** rules, enforced by the backend when settings are saved (the admin form gives the same feedback immediately):

1. `--color-on-brand` is **computed**, not chosen: white or near-black, whichever gives the higher contrast against the brand color.
2. The brand color is **rejected** if neither gives ≥ 4.5:1 (normal text on buttons). The error names the problem and suggests the nearest lighter or darker shade that passes.
3. The brand color must reach ≥ 3:1 against the page background, because it is also used for links, selected borders, and icons. If it does not, links fall back to `--color-text` with an underline, and the admin is warned (not blocked).
4. `--color-brand-subtle` is computed by mixing the brand color with the surface, and is used only behind `--color-text`.
5. The same checks run in a unit test over a shared set of test vectors used by both .NET and Angular, following the pattern of [ADR-03](architecture-decisions.md#adr-03-repository-layout-and-api-contracts).

### 4.4 Delivery to the page

- The SSR server renders brand tokens into a `<style>` block in `<head>` (values come from the resolved store's settings), so the first paint already has the brand colors — no flash of the platform theme.
- Only hex colors and enumerated values are accepted, and they are written through a strict serializer, never string-concatenated, so a stored setting cannot inject CSS or HTML.
- Theme values are part of the store's cached page; changing them follows the cache rules in [system-behavior §4](system-behavior.md).
- Until the branding milestone (roadmap **B**) ships, every store uses the platform default theme ([roadmap](roadmap.md)).

---

## 5. Localization and direction

Restates [requirements §6](requirements.md#6-localization) as design rules.

| Rule | Design consequence |
|---|---|
| `<html lang dir>` follows the active locale | All layout uses **logical properties** (`margin-inline-start`, `padding-inline`, `inset-inline-end`, `text-align: start`). A lint rule rejects physical `left`/`right` properties in the design-system library and apps. |
| Directional icons mirror in RTL | Arrows, chevrons, "back", progress direction, and the quantity stepper order mirror. Logos, the cart/bag icon, media, checkmarks, and phone numbers do **not** mirror. Icons are marked `mirror` or `fixed` in the icon registry. |
| Western digits in both locales *[DEFAULT]* | Prices and numbers render as `850.00`, also in Arabic. Formatting goes through the i18n/formatting library only; components never format numbers themselves. |
| Inputs accept Arabic-Indic digits | Phone and quantity fields accept `٠١٢…` and normalize on input; the field shows the normalized value on blur. |
| Mixed-language content | When a value's resolved locale differs from the page's, the element gets its own `lang` and `dir`, and uses `unicode-bidi: isolate` so punctuation does not jump. |
| Locale in every URL | A language switch in the header links to the same page in the other locale (only for bilingual stores; hidden for single-locale stores). |

**Currency display [PROPOSED]:** English `EGP 850.00`; Arabic `850.00 ج.م`. Prices are always shown with two decimals in the cart, checkout, and order pages, and may drop `.00` on product cards and listings.

---

## 6. Storefront screens

Global layout: a **header** (menu button on mobile / category links on desktop, logo, search, cart with line count), the page content, and a **footer** (store name, contact links, language switch, "Track your order").

### 6.1 Home
- Header, then a hero (store logo/name and a call to action to the main category), category tiles (top-level categories, 2 columns on mobile), and a "Latest products" grid of 8 published products.
- **[PROPOSED]** The hero has no uploaded banner image in the MVP (banner content is not in requirements); it uses the brand color with the store name. A configurable banner is listed in Section 11.

### 6.2 Category listing
- Breadcrumb (two levels max), category title, product count, sort control (newest, price low → high, price high → low — subject to backend support), and the product grid with pagination ("Load more" button that also updates the URL, so results stay shareable and crawlable).
- Filters and facets are deferred ([requirements §27](requirements.md#27-deferred-features)); no filter UI is built.
- Empty category: "No products here yet" with a link back to Home.

### 6.3 Product card
Image (1:1, lazy-loaded below the fold), name (2 lines max), price (`from EGP X` when variant prices differ), and a **Sold out** badge when every active variant is sold out. No quantities, ever.

### 6.4 Product details
- **Gallery**: swipeable on mobile with dot indicators, thumbnails on desktop. When the shopper picks an option value that has linked images (e.g., `Color = Red`), the gallery switches to those images first ([requirements §7](requirements.md#7-catalog)).
- **Option selector**: one row of chips per option (up to 3). A chip for a combination that does not exist or is unavailable is shown as unavailable (struck through, still focusable, announced as "unavailable"), never hidden ([requirements §7 item 6](requirements.md#7-catalog)).
- **Price** updates to the selected variant's effective price.
- **Stock state** of the selected variant: `In stock` (no label, or a subtle check), `Low stock` (warning label "Low stock"), `Sold out` (button replaced by a disabled-looking "Sold out" state). **Exact quantities are never shown** ([requirements §7](requirements.md#7-catalog), stock visibility).
- **Quantity stepper**: maximum from store configuration (default 10), never hardcoded ([requirements §9.4](requirements.md#94-cart-lines)).
- **Add to cart**: sticky bottom bar on mobile (price + button). Each tap carries a client request id; the button shows a spinner while pending and a confirmation toast with "View cart" afterwards ([requirements §9.4](requirements.md#94-cart-lines)).
- **Shipping and payment note** under the button: "Cash on delivery · Flat shipping EGP X · Free over EGP Y" (values from store settings).
- Description (plain text, collapsed after ~6 lines with "Show more"). Omitted entirely when empty ([requirements §6 fallback](requirements.md#6-localization)).

### 6.5 Search
- Search field in the header opens a full-screen search on mobile. Results reuse the product grid. Empty state suggests checking spelling and links to categories.

### 6.6 Cart (full page)
- **Lines**: image, name, option labels, unit price, quantity stepper, line total, remove. A "price changed" note when the stored seen-price differs ([requirements §9.4](requirements.md#94-cart-lines)).
- **Flagged lines** stay visible with a reason and an action, are excluded from totals, and **block checkout** until fixed ([requirements §9.5](requirements.md#95-validation)): "Sold out — Remove", "Only N available — Adjust to available" (the single place an exact quantity may appear), "No longer available — Remove".
- **Summary** (from the backend quote): subtotal, shipping, total, and **[PROPOSED]** a free-shipping progress line ("Add EGP 150 for free shipping") computed from the quote's subtotal and the store's threshold.
- **Merge notice** after login lists any adjustments ([requirements §9.6](requirements.md#96-merge-on-login)).
- Empty cart: "Your cart is empty" with "Continue shopping".

### 6.7 Checkout (single page)
One page, three sections, then a sticky "Place order" bar. Payment is COD only, so there is no payment step ([requirements §10.1](requirements.md#101-checkout-input)).

1. **Contact** — name, phone (fixed `+20` prefix shown as an addon, Egyptian mobile validation with shared test vectors), email (optional for guests). Logged-in customers see values prefilled and editable for this order only, with a note that the profile is not changed.
2. **Delivery** — recipient name and phone (checkbox "Same as contact", checked by default), governorate (searchable select, names in the active locale), city/area, street, building, floor, apartment, landmark, delivery notes ([requirements §15](requirements.md#15-addresses)). Logged-in customers get their latest order's address prefilled.
3. **Review** — the backend quote: lines, subtotal, shipping, total, "Pay cash on delivery".

**Placement behavior**
- The button shows a spinner and stays non-repeatable for the attempt; the idempotency key is reused on retry ([requirements §10.1](requirements.md#101-checkout-input)).
- **Fingerprint mismatch**: the page shows "Your order summary changed" with the differences highlighted, and the shopper must review and press "Place order" again ([requirements §10.2](requirements.md#102-placement-rules)).
- **Validation errors**: an error summary at the top of the form links to each invalid field; each field shows its own message ([Section 8.3](#83-forms-and-errors)).

### 6.8 Order confirmation
- Order number (`#1042`), "We'll call you to confirm", summary, delivery address, and payment method.
- **Guests**: the cancellation link is shown in a highlighted box with "Copy link" and a prompt to save it ([requirements §17.2](requirements.md#172-cancellation-secret-link)). The page sends `Referrer-Policy: no-referrer` and loads no third-party resources.

### 6.9 Order tracking
- **Customers**: Account → Orders (list) → order details with a vertical timeline (`Pending → Confirmed → Processing → Shipped → Delivered`), courier name and tracking number when recorded, items and totals from the snapshot, and **Cancel order** only while `Pending`.
- **Guest lookup** ("Track your order" in the footer): order number + phone in a form posted in the request body. A generic "We couldn't find that order" on any mismatch. The result shows only the masked view from [requirements §17.1](requirements.md#171-status-lookup-read-only).
- **Terminal states**: `Cancelled` and `DeliveryFailed` show a neutral end state with the reason, not a broken timeline.
- **Cancel dialog**: reason as radio buttons (customer reasons from [requirements §11.1](requirements.md#111-order-status)); a note field appears and becomes required for "Other".

### 6.10 Account
- Register, log in, log out, password recovery ([requirements §16](requirements.md#16-customer-accounts-and-authentication)), profile, and order history. Forms follow Section 8.3. No social login, no saved address book (deferred).

### 6.11 Store status pages
- `Setup` / `Suspended` / unknown-host pages from milestone M1 use the platform theme only, never brand values, and contain no store data beyond what [system-behavior](system-behavior.md) allows.

---

## 7. Admin screens

Neutral platform theme. Fixed layout: **sidebar** (Overview, Orders, Products, Categories, Inventory, Settings; collapses to icons below `md`, to a drawer on mobile), **top bar** (store name, staff menu, language switch). The admin supports `ar` and `en` with full RTL. Dates use **Africa/Cairo** ([requirements §20.1](requirements.md#201-conventions)).

### 7.1 Overview
- Date range selector (default last 30 days).
- Four metric tiles: **Delivered and paid sales**, **Total orders**, **Pending orders** (now), **Cancelled orders**, plus a **Needs replenishment** tile ([requirements §20.2](requirements.md#202-metric-definitions)). Each tile links to the matching filtered list.
- Recent orders table (10 rows).
- Tile labels show the exact definition in a tooltip/info popover so numbers are never misread.

### 7.2 Orders list
- **Saved filter tabs**: All · Pending · Pending > 48 h · Delivered, cash not collected · Failed delivery, return not recorded ([requirements §21](requirements.md#21-admin-capabilities)).
- Search by order number, customer name, or phone; filters for order status and payment status.
- Columns: order number, placed at, customer, phone, total, **order status badge**, **payment status badge** (always two separate badges — they are independent fields, [requirements §11](requirements.md#11-order-status-and-payment-status)).
- Status badge colors **[PROPOSED]**: Pending = warning, Confirmed / Processing = info, Shipped = info (outlined), Delivered = success, Cancelled = neutral, DeliveryFailed = danger. Payment: Pending = neutral, Paid = success, Voided = neutral (struck).

### 7.3 Order details
- **Header**: number, status and payment badges, and **only the transitions allowed from the current status** as buttons (from the table in [requirements §11.1](requirements.md#111-order-status)). No disabled buttons for impossible transitions.
- **Transition dialogs**: every transition asks for confirmation (no undo). Cancel and DeliveryFailed require a reason from the staff list, note required for "Other". Ship asks for optional courier name and tracking number. Mark as paid asks for the collection date (default today, validated range).
- **Panels**: contact, delivery details (editable while `Pending`/`Confirmed`/`Processing`), items (editable per [requirements §12](requirements.md#12-order-editing)), totals, courier details, **event log** as a timeline with actor and before/after values, and **internal notes** (clearly labeled "Only visible to staff").
- **Payment correction** is a separate, visually de-emphasized action requiring password re-entry and a reason ([requirements §11.2](requirements.md#112-payment-status-cod)).
- **Record return** per line, with quantities restocked.

### 7.4 Products
- List with search (name/SKU), status filter (Draft, Published, Archived), thumbnail, price range, stock state summary.
- **Editor** sections: Details (translation tabs `العربية` / `English`; the store's default locale is marked "Required"), Images (upload, reorder, link to option value), Options (up to 3, ordered), **Variant grid** (one row per combination: SKU, price override, active toggle, on hand / committed / available read-only with a link to Inventory), Category, and Publication status.
- Unsaved-changes guard on navigation.

### 7.5 Categories
Two-level tree list with archive/restore, and a translation-tab editor like products.

### 7.6 Inventory
Table of tracked variants with **on hand / committed / available**, state filters (In stock, Low stock, Sold out, Discrepancy), an **Adjust stock** dialog (signed quantity + reason; cannot go below committed), and a movement history drawer per variant.

### 7.7 Settings
Store name, logo, theme ([Section 4](#4-brand-theming-proposed-resolution-of-oq1)) with a **live preview** of a product card and buttons in both locales and the contrast check result, supported/default locales (with the "missing required content" report when changing the default — [requirements §6](requirements.md#6-localization)), shipping fee, free-shipping threshold, low-stock threshold.

---

## 8. Shared patterns

### 8.1 Loading
Skeletons shaped like the final content for lists and product pages; spinners only inside buttons. SSR pages render real content on first load, so skeletons appear only on client-side navigation.

### 8.2 Empty states
A short headline naming the space, one sentence, and one action. No illustrations in the MVP (bundle size).

### 8.3 Forms and errors
- Labels always visible above fields (no placeholder-only labels).
- Validate on blur, then on change once a field has shown an error; never on the first keystroke.
- On submit failure: an error summary at the top with links to fields, focus moved to the summary, and per-field messages connected through `aria-describedby`.
- Backend errors are mapped to field messages through the error codes in the domain model; unknown errors show a generic message with a retry, never raw text.
- All messages exist in `ar` and `en` in the i18n library.

### 8.4 Notifications in the UI
Toasts for confirmations ("Added to cart"), announced through the CDK live announcer, auto-dismissed after 4 s, and never used for errors that need action — those stay inline.

### 8.5 Confirmation dialogs
Used for every irreversible action (order transitions, archive). The confirm button names the action ("Cancel order", not "OK"); destructive actions use the danger color.

---

## 9. Accessibility

Target: WCAG 2.1 AA ([requirements §23](requirements.md#23-non-functional-requirements)).

- Keyboard: every action reachable and visible focus on every interactive element (`--color-focus`, 2 px ring + offset, never removed).
- Contrast: platform tokens meet AA; brand tokens are enforced by Section 4.3.
- Semantics: one `h1` per page, landmarks (`header`, `nav`, `main`, `footer`), real `button`/`a` elements, option chips as a radio group.
- Images: product images use the product name as alt text by default; decorative images have empty alt.
- Motion: respects `prefers-reduced-motion`.
- Testing: `axe` checks in component tests and in E2E on key pages, run in both `ar` and `en`; a manual screen-reader pass (TalkBack on Android, NVDA on Windows) per milestone that adds UI.

---

## 10. Performance budget

**[PROPOSED]** budgets for storefront public pages, measured on a mid-range Android phone over 4G ([requirements §23](requirements.md#23-non-functional-requirements)):

| Metric | Budget |
|---|---|
| LCP | ≤ 2.5 s |
| INP | ≤ 200 ms |
| CLS | ≤ 0.1 |
| Initial JS (storefront, gzipped) | ≤ 150 KB |
| Fonts | ≤ 2 files per script, subset, preloaded for the active locale only |
| Images | Responsive `srcset`, modern formats (AVIF/WebP), explicit width and height |

Techniques: SSR with hydration, lazy-loaded routes and `@defer` blocks for below-the-fold content, no UI library in the storefront bundle, and Lighthouse CI budgets that fail the build when exceeded.

---

## 11. Open design questions

| # | Question | Proposed answer |
|---|---|---|
| UQ1 | Approve the theming scope in Section 4 as the resolution of **OQ1**? | Yes — record in the requirements decision log once approved |
| UQ2 | Should the Home hero support an uploaded banner image in the MVP? | No — deferred; uses brand color and store name |
| UQ3 | Sort options on category pages — which does the backend support in the MVP? | Newest, price ascending, price descending |
| UQ4 | Show `.00` on product cards? | No on cards and listings; yes in cart, checkout, and orders |
| UQ5 | Dark mode for the storefront? | Deferred — brand colors are validated against a light background only |
| UQ6 | Should the admin default to the staff user's last-used locale or the store's default locale? | Last-used, stored per staff user |

Deferred design features (not in the MVP): custom CSS, layout or section customization, per-brand components, banners and promotions, filters and facets UI, color swatches (already deferred in [requirements §27](requirements.md#27-deferred-features)), wishlists, reviews.
