# TechXpress: UI/UX Design

## 1. Design Goals

TechXpress is an Arabic-first marketplace for electronics. The interface is designed to make browsing, checkout, and order tracking feel fast and trustworthy for customers, while giving vendors and administrators dense but readable operational screens.

| Goal | How it is achieved |
|---|---|
| Arabic-first, RTL | Every customer, vendor, and admin screen is right-to-left; Arabic labels, currency, dates, and validation messages |
| Trust at checkout | Clear order summary, Stripe test-mode redirect, explicit payment states, cart preserved on failure |
| Low cognitive load | One primary action per screen, grouped cards, progressive disclosure for optional fields |
| Mobile-first | Single-column stacking, sticky primary actions, touch targets of at least 44px |
| Consistent feedback | Toastr after POST actions, inline Bootstrap alerts for auth/validation, dedicated error pages |
| Role clarity | Separate customer, vendor, and admin navigation; protected routes redirect to access-denied |

## 2. Brand and Visual Language

### 2.1 Color Palette

| Token | Hex | Usage |
|---|---|---|
| Navy | `#0C223F` | Primary brand, headers, primary buttons, headings |
| Navy dark | `#071528` | Hero gradients, footer |
| Navy light | `#173860` | Gradients, hover states |
| Cream | `#FAF7F2` | Page background, soft surfaces |
| Beige | `#FAF7F2` / `#EDE6DB` | Button text on navy, hover fills |
| Border | `#E3D9C9` | Card borders, dividers, input borders |
| White | `#FFFFFF` | Cards, tables, forms |
| Text dark | `#0C223F` | Body and headings |
| Text muted | `#64748B` | Secondary labels, helper text |
| Success | `#15803D` | Positive amounts, paid/delivered badges |
| Warning | `#F59E0B` | Pending states, platform commission accent |
| Danger | `#B91C1C` | Cancelled, destructive actions, validation errors |
| Purple | `#6D28D9` | Returned orders |

### 2.2 Typography

- Primary font: **Cairo** (Google Fonts), weights 400, 500, 600, 700, 800.
- Fallback: `"Segoe UI", sans-serif`.
- Headings use 700–800 weight; body text uses 400–500; table headers and badges use 600–700.
- Numeric values use tabular alignment where possible; prices always show two decimals.

### 2.3 Shape, Spacing, and Depth

- Corner radii: 8px (badges/inputs), 12–16px (cards), 18–24px (hero panels).
- Spacing scale: 4, 8, 12, 16, 20, 24, 32, 40, 48px.
- Cards use a 1px border plus a soft shadow (`0 4px 16px rgba(12,34,63,.04)`); primary panels use a deeper shadow.
- Gradients are reserved for hero sections, wallet/credit cards, and the platform commission panel.

## 3. Layout and Navigation

### 3.1 Customer Layout

- Sticky header: logo, primary navigation (Home, Products, Cart), search, wishlist, account menu.
- Cart badge updates asynchronously after add-to-cart.
- Footer: quick links, contact details, payment security note, copyright.
- Content width: Bootstrap container, max 1320px; page sections use 24px vertical rhythm.

### 3.2 Vendor Layout

- Left-side navigation (`_VendorNav`) with dashboard, products, orders, wallet, and store profile.
- Top bar shows vendor identity and logout.
- Operational tables use DataTables with Arabic search, pagination, and page-length controls.

### 3.3 Admin Layout

- Dark navy sidebar with grouped sections: Overview, Catalog, Operations, Users, Messages.
- Dashboard uses KPI cards followed by financial analytics, best-selling products, purchasing regions, expense entry, and recent orders.
- Destructive actions are separated visually (outline danger) and confirmed before submission.

## 4. Component Library

| Component | Behaviour |
|---|---|
| Primary button | Navy fill, beige text, 10px radius, hover lift |
| Outline button | Transparent with border, used for secondary navigation |
| Danger button | Red outline/fill for cancel and delete actions |
| Status badge | Pill with icon + label; colors map to `OrderStatus` and `ShippingStatus` |
| Summary card | Label/value rows, divider before total, credit and discount rows in green |
| Form field | Label above input, helper text below, red border and Arabic message on error |
| Data table | Sticky header on scroll, zebra-free clean rows, action column on the left (RTL) |
| Empty state | Icon, short title, explanation, and a recovery action |
| Confirmation | Native `confirm()` for destructive POST actions, with Arabic explanation of side effects |
| Toast | Top-left Toastr notification after redirects; success green, error red |
| Inline alert | Bootstrap alert used on auth pages and payment error pages |

## 5. Screen Inventory

### 5.1 Customer Screens

| Screen | Key elements |
|---|---|
| Home | Hero with featured products, category tiles, featured product grid, trust strip |
| Products | Search, category filter, sorting, responsive product grid, pagination |
| Product details | Image, price, stock, vendor, description, add-to-cart, reviews (verified buyers only) |
| Cart | Item rows, quantity update, remove, subtotal, checkout CTA, empty state |
| Checkout | Shipping address, promo code, optional store-credit box with available balance, live amount due, order summary |
| Stripe redirect | Full-page redirect; no card data stored by TechXpress |
| Payment success | Confirmation, order number, links to order details and shopping |
| Payment cancelled | Explanation, retained cart, retry action |
| Payment error | Dedicated error page when Stripe session creation fails; retry and support links |
| Login / Register | Split auth layout, Google button, inline alerts, vendor-application link |
| Profile hub | User card, store-credit balance card, quick actions, account navigation |
| Orders | Status badges, total order value, cancel for Pending/Paid, return for Delivered |
| Order details | Items, shipping status per vendor, payment method (Stripe or store credit), credit-used row, cancel/return actions |
| Credit ledger | Available balance hero, transaction table with type, description, balance after, signed amount |
| Wishlist | Saved products with add-to-cart and remove |

### 5.2 Vendor Screens

| Screen | Key elements |
|---|---|
| Dashboard | Sales, order counts, wallet balance, recent orders |
| Products | DataTable, create/edit forms, soft-delete/restore, image upload |
| Orders | Per-item shipping status and next-step action (Pending → Preparing → Shipped → Delivered) |
| Wallet | Balance, transaction history, payout request form, request history |
| Store profile | Store name, description, logo, contact data |
| Vendor application | Store details, national ID, commercial register, tax card upload, status page |

### 5.3 Admin Screens

| Screen | Key elements |
|---|---|
| Dashboard | Year filter; confirmed sales, platform commission, vendor payable, expenses, net profit; best-selling products; highest/lowest regions; expense entry and history; product/order/vendor/customer counts; recent orders |
| Orders | DataTable, status dropdown with allowed transitions, cancel, return for delivered orders, details |
| Order details | Customer, shipping address, items, payment ID, subtotal/discount/credit/cash, status update, return action |
| Products | DataTable, create/edit, soft delete/restore, category and vendor assignment |
| Categories | DataTable, create/edit/delete |
| Users | Customer/vendor/admin list, suspend/activate |
| Vendor applications | Review documents, approve/reject with notes |
| Payout requests | Approve/reject, audit fields |
| Contact messages | Inbox-style list and detail view |

## 6. Key User Flows

### 6.1 Purchase Without Store Credit

1. Customer adds products to the cart.
2. Checkout shows the summary and the amount due.
3. Order is created as `Pending`; the cart is kept.
4. Customer is redirected to Stripe test checkout.
5. On success, the payment is confirmed, the order becomes `Paid`, and the cart is cleared.
6. On cancel/failure, the cart stays intact and the order can be retried from the payment page.

### 6.2 Purchase With Store Credit

1. Checkout displays the available credit and an optional checkbox.
2. Checking the box recalculates the amount due immediately.
3. The order stores `StoreCreditUsed`; the credit is deducted and logged in the ledger.
4. If credit covers the full total, the order is created `Paid` with payment reference `STORE_CREDIT` and no Stripe redirect.
5. Otherwise, only the remaining cash amount is sent to Stripe.

### 6.3 Cancel and Return Refunds

- **Pending order:** cancel restores stock and returns only the reserved credit.
- **Paid or Shipped order:** cancel restores stock, refunds the full order value (cash + used credit) as store credit, reverses vendor earnings and platform commission.
- **Delivered order:** the customer uses **Return**; the full value is refunded as store credit, stock is restored, and vendor/platform earnings are reversed. The status becomes `Returned`.

### 6.4 Vendor Shipping

- Vendors advance each order item independently: Pending → Preparing → Shipped → Delivered.
- The parent order advances automatically only when all items reach the same stage.
- Cancelled and returned orders show a terminal notice and cannot be shipped.

## 7. Responsive Behaviour

| Breakpoint | Behaviour |
|---|---|
| ≥ 1200px | Full sidebar layouts, multi-column dashboards, 4-column product grids |
| 768–1199px | Collapsed navigation, 2–3 column grids, stacked summary panels |
| < 768px | Single-column cards, full-width buttons, horizontal-scroll tables, hidden non-essential table columns |
| < 576px | Reduced hero typography, compact KPI cards, sticky checkout action |

## 8. Accessibility and RTL Rules

- `dir="rtl"` and `lang="ar"` on the document.
- Logical spacing (`ms-*`, `me-*`) instead of left/right utilities.
- Visible focus states on links, buttons, and form controls.
- Color is never the only signal: statuses pair color with icon and text.
- Form inputs have explicit labels, `aria-label` on icon-only controls, and Arabic validation messages.
- Images include meaningful `alt` text; decorative icons use `aria-hidden`.
- Keyboard order follows visual order; tables expose headers and captions.

## 9. Feedback, Errors, and Trust

- POST results are surfaced through `TempData` and rendered by a shared Toastr partial.
- Validation errors are shown inline in Arabic; auth pages use Bootstrap alerts.
- Unhandled exceptions are logged with method, path, query, user, IP, trace ID, and stack trace, then shown on a friendly error page.
- Stripe failures redirect to a dedicated payment error page, never to a raw exception page.
- Payment and refund copy explains exactly what happened to the money and the cart.

## 10. Implementation Notes

- ASP.NET Core MVC Razor views with Bootstrap 5, custom CSS, and Cairo font.
- jQuery for cart updates, checkout totals, and confirmation dialogs.
- DataTables for admin/vendor lists; Toastr for notifications; Font Awesome for icons.
- No client-side SPA framework; navigation is server-rendered for reliability and SEO.
- Shared partials: `_Layout`, `_VendorNav`, `_ToastrNotifications`, `_ProductCardsPartial`, `_ProductResultsPartial`.
- Design tokens are defined per page or in `wwwroot/css/site.css`; page-specific styles are scoped in the Razor view to avoid regressions.
