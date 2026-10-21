## 1. Stakeholder Analysis

| Stakeholder | Description | Needs / Expectations | Influence |
|---|---|---|---|
| **Customer** | Visitor or registered user buying electronics | Easy browsing and search, simple cart, secure payment, order tracking | High |
| **Vendor** | Seller offering products on the platform | Easy onboarding, document upload, catalog management, transparent wallet balance tracking, and reliable payout requests | High |
| **Administrator** | Store staff managing catalog, orders, vendors, and payouts | Fast management tools, secure document verification, automated commission tracking, and payout settlement control | High |
| **Store Owner** | Business owner | Reliable sales, accurate stock, trusted vendors, automated platform commission, legally compliant multi-vendor marketplace | High |
| **Payment Provider (Stripe)** | External payment gateway | Correct API usage, secure handling of payment data and checkout sessions | Medium |
| **Development Team** | 5 students | Clear scope, maintainable N-Tier code, isolated feature slices | High |
| **Lecturer / Evaluators** | DEPI supervisors | Compliance with specifications, robust documentation, working deployment | High |

---

## 2. User Stories & Use Cases

### 2.1 User Stories

**Customer**

| ID | Story | Priority |
|---|---|---|
| US-01 | As a visitor, I want to browse products so that I can see what the store sells. | High |
| US-02 | As a visitor, I want to search products by name so that I can find an item quickly. | High |
| US-03 | As a visitor, I want to filter products by category so that I can narrow results. | High |
| US-04 | As a visitor, I want to view product details so that I can decide to buy. | High |
| US-05 | As a visitor, I want to register an account so that I can place orders. | High |
| US-06 | As a user, I want to log in and out securely. | High |
| US-07 | As a customer, I want to add products to a cart so that I can review them before checkout. | High |
| US-08 | As a customer, I want to change quantities or remove items from my cart. | High |
| US-09 | As a customer, I want to apply a discount/promo code so that I get a lower total. | Medium |
| US-10 | As a customer, I want to check out and pay by card so that my order is completed. | High |
| US-11 | As a customer, I want to see my order history and statuses. | High |
| US-12 | As a customer, I want to update my personal details and password. | Medium |

**Vendor**

| ID | Story | Priority |
|---|---|---|
| US-18 | As a vendor, I want to submit my National ID, Commercial Register, and Tax Card so that my account can be verified. | High |
| US-19 | As a vendor, I want to track my verification status (Pending, Approved, Rejected) and see rejection reasons if any. | High |
| US-20 | As an approved vendor, I want to list and manage my products on the platform. | High |
| US-22 | As an approved vendor, I want to view my current wallet balance and transaction history so that I can track net earnings after platform commissions. | High |
| US-23 | As an approved vendor, I want to submit payout requests for my available wallet balance so that I can withdraw earnings. | High |

**Administrator**

| ID | Story | Priority |
|---|---|---|
| US-13 | As an admin, I want to log in to a protected admin panel. | High |
| US-14 | As an admin, I want to add, edit, and delete products (with image) so that the catalog stays current. | High |
| US-15 | As an admin, I want to manage categories. | High |
| US-16 | As an admin, I want to view all orders and change their status (Shipped, Delivered, Cancelled). | High |
| US-17 | As an admin, I want sortable, searchable, paginated tables to manage data quickly. | Medium |
| US-21 | As an admin, I want to review submitted vendor legal documents and approve or reject their store requests. | High |
| US-24 | As an admin, I want to review vendor payout requests, verify bank settlements, and approve or reject withdrawal transactions. | High |

### 2.2 Main Use Cases

| ID | Use Case | Actor | Description |
|---|---|---|---|
| UC-01 | Browse and Search Products | Visitor, Customer | View product list, search by name, filter by category |
| UC-02 | View Product Details | Visitor, Customer | Open a product details page |
| UC-03 | Register | Visitor | Create a customer or vendor account |
| UC-04 | Login / Logout | Customer, Vendor, Admin | Authenticate with email and password |
| UC-05 | Manage Cart | Customer | Add, update quantity, remove items, apply promo code |
| UC-06 | Checkout and Place Order | Customer | Enter shipping details and confirm order |
| UC-07 | Pay with Stripe & Distribute Wallet Funds | Customer, Stripe | Process payment; system auto-calculates commission and credits net vendor earnings |
| UC-08 | View Order History | Customer | List past orders and their details |
| UC-09 | Manage Profile | Customer | Update personal details and change password |
| UC-10 | Manage Products | Admin, Approved Vendor | Create, read, update, delete products |
| UC-11 | Manage Categories | Admin | Create, read, update, delete categories |
| UC-12 | Manage Orders | Admin | View orders and update status |
| UC-13 | Submit Verification Documents | Vendor | Upload National ID, Commercial Register, and Tax Card |
| UC-14 | Review & Verify Vendor Status | Admin | Inspect documents and approve/reject vendor requests |
| UC-15 | View Wallet & Financial History | Approved Vendor | View `WalletBalance` and ledger of deposit/withdrawal transactions |
| UC-16 | Request Wallet Payout | Approved Vendor | Submit withdrawal request for available wallet balance |
| UC-17 | Process Vendor Payout Requests | Admin | Review pending payout requests, confirm transfer, and settle transactions |

---

## 3. Functional Requirements

### 3.1 Authentication & Authorization
| ID | Requirement |
|---|---|
| FR-01 | The system shall allow users to register with name, email, and password. |
| FR-02 | The system shall assign the role **Customer** or **Vendor** upon registration. |
| FR-03 | The system shall allow users to log in and log out. |
| FR-04 | The system shall support roles: Customer, Vendor, and Admin. |
| FR-05 | The system shall restrict the Admin Panel, vendor approvals, and payout processing to the Admin role. |
| FR-06 | The system shall show an "Access Denied" page for unauthorized access. |

### 3.2 Product Catalog
| ID | Requirement |
|---|---|
| FR-07 | The system shall display a list of active products with name, price, image, and category. |
| FR-08 | The system shall allow searching products by name. |
| FR-09 | The system shall allow filtering products by category. |
| FR-10 | The system shall support pagination of the product list. |
| FR-11 | The system shall display product details (description, price, stock, vendor name). |
| FR-12 | The system shall show an out-of-stock indication when stock is zero. |
| FR-13 | The system shall provide live search using jQuery. |

### 3.3 Shopping Cart
| ID | Requirement |
|---|---|
| FR-14 | The system shall store the cart in Session State. |
| FR-15 | The system shall allow adding, removing, and updating quantity of cart items. |
| FR-16 | The system shall calculate the cart total in the Business Logic Layer. |
| FR-17 | The system shall support promotions/discounts applied to the cart total. |
| FR-18 | The system shall prevent adding more than the available stock. |

### 3.4 Orders & Payment
| ID | Requirement |
|---|---|
| FR-19 | The system shall provide a checkout flow that collects shipping information. |
| FR-20 | The system shall save the order and reduce product stock in a single atomic transaction using the Unit of Work. |
| FR-21 | The system shall store the unit price and supplying `VendorId` of each product at purchase time inside `OrderDetails`. |
| FR-22 | The system shall integrate Stripe to process payments securely. |
| FR-23 | The system shall store payment transaction details (payment reference) with the order. |
| FR-24 | The system shall update the order status to Paid after a successful payment. |
| FR-25 | The system shall support the order statuses: Pending, Paid, Shipped, Delivered, Cancelled. |

### 3.5 User Profile
| ID | Requirement |
|---|---|
| FR-26 | The system shall let customers view and update their personal details. |
| FR-27 | The system shall let customers view their order history and order details. |
| FR-28 | The system shall let customers change their password. |
| FR-29 | The system shall prevent a customer from viewing another customer's orders. |

### 3.6 Admin Panel
| ID | Requirement |
|---|---|
| FR-30 | The system shall let admins create, edit, and delete products, including image upload. |
| FR-31 | The system shall let admins create, edit, and delete categories. |
| FR-32 | The system shall let admins view all orders and update order status. |
| FR-33 | The system shall present products, categories, orders, vendors, and payout requests in DataTables with sorting, filtering, and pagination. |
| FR-34 | The system shall show success/failure notifications using Toastr. |
| FR-35 | The system shall prevent deleting a category that contains products, and a product that appears in past orders. |

### 3.7 Vendor Verification & Store Management
| ID | Requirement |
|---|---|
| FR-36 | The system shall allow vendors to submit store details and upload legal documents (National ID, Commercial Register, Tax Card). |
| FR-37 | The system shall set a new vendor's status to Pending upon submission, preventing product listing until approved. |
| FR-38 | The admin panel shall display a list of pending vendor requests and allow secure preview of uploaded documents. |
| FR-39 | The admin shall be able to approve a vendor (activating their store) or reject them with a specified reason. |

### 3.8 Vendor Wallet & Financial Settlement
| ID | Requirement |
|---|---|
| FR-40 | The system shall automatically calculate platform commissions and credit net earnings to the vendor's `WalletBalance` upon order payment. |
| FR-41 | The system shall maintain an immutable financial ledger table (`WalletTransactions`) recording `Deposit` and `Withdrawal` entries with statuses (`Pending`, `Completed`, `Rejected`). |
| FR-42 | The system shall allow approved vendors to view their current `WalletBalance` and ledger history on their dashboard. |
| FR-43 | The system shall allow approved vendors to submit payout requests provided the requested amount does not exceed their available `WalletBalance`. |
| FR-44 | The admin panel shall present pending payout requests and allow administrators to approve or reject requests after confirming manual bank/wallet transfers. |
| FR-45 | Upon payout approval, the system shall deduct the requested amount from the vendor's `WalletBalance` and mark the transaction as `Completed`. |

---

## 4. Non-Functional Requirements

### 4.1 Performance
| ID | Requirement |
|---|---|
| NFR-01 | Catalog pages shall load in under 3 seconds on a normal connection. |
| NFR-02 | Search and filtering shall return results in under 1 second for the expected catalog size. |
| NFR-03 | Database queries shall use async operations to avoid blocking the server. |

### 4.2 Security
| ID | Requirement |
|---|---|
| NFR-04 | Passwords shall be hashed (ASP.NET Identity default); never stored as plain text. |
| NFR-05 | All state-changing requests shall be protected against CSRF using anti-forgery tokens. |
| NFR-06 | User input shall be validated server-side; output shall be HTML-encoded to prevent XSS (Razor default). |
| NFR-07 | Data access shall use EF Core parameterized queries to prevent SQL injection. |
| NFR-08 | Card data shall never be stored or processed by the application; Stripe handles it. |
| NFR-09 | Secrets (Stripe keys, connection strings) shall not be committed to source control. |
| NFR-10 | Vendor legal document uploads shall be stored in secure, non-public storage directories or restricted endpoints. |
| NFR-11 | The site shall be served over HTTPS in production. |

### 4.3 Usability
| ID | Requirement |
|---|---|
| NFR-12 | The UI shall be responsive on mobile, tablet, and desktop (Bootstrap 5). |
| NFR-13 | Users shall receive clear feedback messages for actions and errors (Toastr JS). |
| NFR-14 | Navigation shall be consistent across the store, vendor portal, and admin panel. |

### 4.4 Reliability
| ID | Requirement |
|---|---|
| NFR-15 | Order placement and wallet commission distribution shall be atomic: either the order, stock update, and wallet credit all succeed or none does. |
| NFR-16 | The system shall log errors using the built-in `ILogger`. |
| NFR-17 | The deployed system shall target at least 99% availability. |

### 4.5 Maintainability
| ID | Requirement |
|---|---|
| NFR-18 | The solution shall follow N-Tier Architecture (Web / Services / Data) with Vertical Slice feature modules. |
| NFR-19 | Data access shall go through the Repository and Unit of Work patterns. |
| NFR-20 | Dependencies shall be injected (Dependency Injection); controllers shall not access the database directly. |
| NFR-21 | Code shall follow SOLID principles and consistent naming conventions. |
| NFR-22 | Public APIs shall have XML documentation. |

### 4.6 Testability
| ID | Requirement |
|---|---|
| NFR-23 | Business logic (including wallet calculations and commission deductions) shall be covered by unit tests. |
| NFR-24 | Repositories shall be covered by integration tests (xUnit + SQLite / InMemory). |
| NFR-25 | Critical user paths shall be covered by basic UI testing. |

### 4.7 Portability & Deployment
| ID | Requirement |
|---|---|
| NFR-26 | The application shall run on .NET 8 and SQL Server. |
| NFR-27 | The application shall be deployable to Microsoft Azure App Service and Azure SQL Database. |
