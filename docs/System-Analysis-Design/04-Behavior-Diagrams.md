# TechXpress: Data Flow & System Behavior

## 1. DFD Level 0 (Context Diagram)

```mermaid
flowchart LR
    CUS["Customer"]
    VEN["Vendor"]
    ADM["Admin"]
    STR["Stripe"]
    SYS(("TechXpress<br/>System"))

    CUS -->|registration, login, search, cart actions, order details| SYS
    SYS -->|products, cart, order confirmation, order history| CUS
    VEN -->|"verification docs (ID, Commercial Register, Tax Card)"| SYS
    VEN -->|payout request, product catalog management| SYS
    SYS -->|verification status, wallet balance, payout history| VEN
    ADM -->|product, category, order, vendor & payout management| SYS
    SYS -->|reports, pending vendor list, pending payout requests| ADM
    SYS -->|payment session request| STR
    STR -->|payment result| SYS
```

## 2. DFD Level 1

```mermaid
flowchart LR
    CUS["Customer"]
    VEN["Vendor"]
    ADM["Admin"]
    STR["Stripe"]

    P1(("1.0<br/>Authentication"))
    P2(("2.0<br/>Browse Catalog"))
    P3(("3.0<br/>Manage Cart"))
    P4(("4.0<br/>Place Order"))
    P5(("5.0<br/>Process Payment & Wallet Distribution"))
    P6(("6.0<br/>Manage Store, Verification & Payouts - Admin"))
    P7(("7.0<br/>Vendor Onboarding, Store & Payout Request"))

    D1[("D1 Users, Roles, Vendors")]
    D2[("D2 Products and Categories")]
    D3[("D3 Orders and OrderDetails")]
    D4[("D4 Session Cart")]
    D5[("D5 WalletTransactions")]

    CUS -->|credentials| P1
    VEN -->|credentials| P1
    ADM -->|credentials| P1
    P1 <-->|verify, create| D1
    P1 -->|auth result| CUS

    CUS -->|search, filter| P2
    P2 <-->|read| D2
    P2 -->|product list, details| CUS

    CUS -->|add, update, remove, promo code| P3
    P3 <-->|cart items| D4
    P3 -->|read prices and stock| D2
    P3 -->|cart summary| CUS

    CUS -->|shipping address| P4
    P4 -->|read cart| D4
    P4 -->|create order, reduce stock| D3
    P4 -->|update stock| D2
    P4 -->|order| P5

    P5 -->|session request| STR
    STR -->|payment result| P5
    P5 -->|status Paid, payment reference| D3
    P5 -->|credit net vendor earnings, platform commission| D1
    P5 -->|create Deposit transaction| D5
    P5 -->|confirmation| CUS

    VEN -->|upload ID, Commercial Register, Tax Card| P7
    VEN -->|request payout| P7
    P7 <-->|save pending documents, check balance| D1
    P7 -->|create Withdrawal transaction| D5

    ADM -->|CRUD products, categories| P6
    ADM -->|review & approve/reject vendors| P6
    ADM -->|approve/reject payout requests| P6
    ADM -->|change order status| P6
    P6 <-->|read, write| D1
    P6 <-->|read, write| D2
    P6 <-->|read, write| D3
    P6 <-->|update transaction status| D5

    CUS -->|update details| P7
    P7 <-->|read, write| D1
    P7 -->|read order history| D3
    P7 -->|profile and history| CUS
```

## 3. Sequence Diagrams

### 3.1 Vendor Onboarding & Verification Flow

```mermaid
sequenceDiagram
    actor V as Vendor
    participant VC as VendorController
    participant VS as VendorService
    participant UOW as UnitOfWork
    actor A as Admin
    participant AC as Admin VendorsController
    participant DB as Database

    V->>VC: POST /Vendor/SubmitDocuments (National ID, Register, Tax Card)
    VC->>VS: SubmitDocumentsAsync(vendorDto)
    VS->>VS: Save documents to wwwroot/uploads
    VS->>UOW: Vendors.AddAsync(vendor with status Pending)
    VS->>UOW: SaveChangesAsync()
    UOW->>DB: INSERT into Vendors
    VS-->>VC: Success
    VC-->>V: Display "Pending Verification" message

    A->>AC: GET /Admin/Vendors/Pending
    AC->>VS: GetPendingVendorsAsync()
    VS->>UOW: Vendors.GetPendingVendorsAsync()
    UOW->>DB: SELECT Vendors WHERE Status = Pending
    DB-->>AC: List of pending vendors
    AC-->>A: Display pending vendors with document previews

    alt Admin Approves Vendor
        A->>AC: POST /Admin/Vendors/Approve (vendorId)
        AC->>VS: VerifyVendorAsync(vendorId, isApproved: true)
        VS->>UOW: Vendors.GetByIdAsync(vendorId)
        VS->>VS: Update Status = Approved
        VS->>UOW: SaveChangesAsync()
        UOW->>DB: UPDATE Vendors SET Status = Approved
        AC-->>A: Toastr success message
    else Admin Rejects Vendor
        A->>AC: POST /Admin/Vendors/Reject (vendorId, reason)
        AC->>VS: VerifyVendorAsync(vendorId, isApproved: false, reason)
        VS->>UOW: Vendors.GetByIdAsync(vendorId)
        VS->>VS: Update Status = Rejected, set RejectionReason
        VS->>UOW: SaveChangesAsync()
        UOW->>DB: UPDATE Vendors SET Status = Rejected
        AC-->>A: Toastr rejection notice sent
    end
```

### 3.2 Login

```mermaid
sequenceDiagram
    actor U as User
    participant AC as AccountController
    participant SM as SignInManager (Identity)
    participant DB as Database

    U->>AC: POST /Account/Login (email, password)
    AC->>SM: PasswordSignInAsync(email, password)
    SM->>DB: Find user and verify password hash
    DB-->>SM: User data
    alt Valid credentials
        SM-->>AC: Succeeded (auth cookie issued)
        AC-->>U: Redirect to Home (Admin to Dashboard)
    else Invalid credentials
        SM-->>AC: Failed
        AC-->>U: Show login page with error
    end
```

### 3.3 Add to Cart

```mermaid
sequenceDiagram
    actor C as Customer
    participant CC as CartController
    participant CS as CartService
    participant UOW as UnitOfWork
    participant SES as Session

    C->>CC: POST /Cart/Add (productId, quantity)
    CC->>CS: AddItem(productId, quantity)
    CS->>UOW: Products.GetByIdAsync(productId)
    UOW-->>CS: Product
    alt Product active and stock sufficient
        CS->>SES: Read cart, add or update item, save cart
        CS-->>CC: Updated cart
        CC-->>C: JSON success (Toastr message, cart count)
    else Out of stock
        CS-->>CC: Error
        CC-->>C: JSON error (Toastr message)
    end
```

### 3.4 Checkout and Payment, and Commission Allocation

```mermaid
sequenceDiagram
    actor C as Customer
    participant OC as CheckoutController
    participant OS as OrderService
    participant UOW as UnitOfWork
    participant PS as PaymentService
    participant WS as WalletService
    participant ST as Stripe
    participant DB as Database

    C->>OC: POST /Checkout (shipping address)
    OC->>OS: PlaceOrderAsync(userId, cart, address)
    OS->>UOW: BeginTransactionAsync()
    loop each cart item
        OS->>UOW: Products.GetByIdAsync(productId)
        UOW->>DB: SELECT product
        OS->>OS: Check stock, reduce StockQuantity, attach VendorId
    end
    OS->>UOW: Orders.AddAsync(order with details & VendorIds)
    OS->>UOW: CommitAsync()
    UOW->>DB: INSERT order, ORDERDETAILS, UPDATE stock
    alt Any step fails
        OS->>UOW: RollbackAsync()
        OS-->>OC: Error
        OC-->>C: Error message, back to cart
    else Success
        OS-->>OC: Order (status Pending)
        OC->>PS: CreateCheckoutSessionAsync(order)
        PS->>ST: Create Checkout Session
        ST-->>PS: Session id and URL
        PS-->>OC: Redirect URL
        OC-->>C: Redirect to Stripe
        C->>ST: Enter card details and pay
        ST-->>C: Redirect to success URL (session id)
        C->>OC: GET /Checkout/Success?session_id
        OC->>PS: ConfirmPaymentAsync(sessionId)
        PS->>ST: Retrieve session (verify paid)
        ST-->>PS: Payment status Paid
        PS->>OS: Mark order Paid, store PaymentIntentId
        
        Note over OS,WS: Distribute Net Profit to Vendor Wallets
        OS->>WS: ProcessOrderCommissionAsync(orderId)
        loop each order detail with VendorId
            WS->>WS: Calculate Net Amount = (UnitPrice * Qty) - PlatformCommission
            WS->>UOW: Vendors.GetByIdAsync(vendorId)
            WS->>WS: Vendor.WalletBalance += Net Amount
            WS->>UOW: Wallets.AddAsync(WalletTransaction Type=Deposit)
        end
        OS->>UOW: SaveChangesAsync()
        UOW->>DB: UPDATE Orders, Vendors, INSERT WalletTransactions
        OC-->>C: Order confirmation page, cart cleared
    end
```

### 3.5 Admin: Update Order Status

```mermaid
sequenceDiagram
    actor A as Admin
    participant OC as Admin OrdersController
    participant OS as OrderService
    participant UOW as UnitOfWork
    participant DB as Database

    A->>OC: POST /Admin/Orders/UpdateStatus (id, status)
    Note over OC: Authorize Roles = Admin and anti-forgery token
    OC->>OS: UpdateStatusAsync(id, status)
    OS->>UOW: Orders.GetByIdAsync(id)
    UOW->>DB: SELECT order
    OS->>OS: Validate allowed transition
    OS->>UOW: Orders.Update(order), SaveChangesAsync()
    UOW->>DB: UPDATE order
    OS-->>OC: Result
    OC-->>A: JSON result (Toastr message, DataTable reload)
```

### 3.6 Vendor Payout Request & Admin Settlement Flow

```mermaid
sequenceDiagram
    actor V as Vendor
    actor A as Admin
    participant VPC as VendorProductController
    participant APC as AdminPayoutController
    participant WS as WalletService
    participant UOW as UnitOfWork
    participant DB as Database

    V->>VPC: POST /Vendor/RequestPayout (amount)
    VPC->>WS: RequestPayoutAsync(vendorId, amount)
    WS->>UOW: Vendors.GetByIdAsync(vendorId)
    alt Requested Amount > WalletBalance or Amount <= 0
        WS-->>VPC: Error ("Insufficient funds")
        VPC-->>V: Toastr error message
    else Balance Sufficient
        WS->>UOW: Wallets.AddAsync(Transaction Type=Withdrawal, Status=Pending)
        WS->>UOW: SaveChangesAsync()
        UOW->>DB: INSERT into WalletTransactions
        WS-->>VPC: Success
        VPC-->>V: Toastr "Payout request submitted"
    end

    A->>APC: GET /Admin/Payouts (Pending Requests)
    APC->>WS: GetPendingPayoutsAsync()
    WS->>UOW: Wallets.GetPendingPayoutsAsync()
    UOW->>DB: SELECT WalletTransactions WHERE Status = Pending
    DB-->>APC: List of pending payouts
    APC-->>A: Display pending payout table

    alt Admin Approves Payout
        A->>APC: POST /Admin/Payouts/Approve (transactionId, referenceNumber)
        APC->>WS: ProcessPayoutAsync(transactionId, approve: true)
        WS->>UOW: Wallets.GetByIdAsync(transactionId)
        WS->>UOW: Vendors.GetByIdAsync(vendorId)
        WS->>WS: Deduct Vendor.WalletBalance -= Amount
        WS->>WS: Update Transaction Status = Completed
        WS->>UOW: SaveChangesAsync()
        UOW->>DB: UPDATE Vendors, WalletTransactions
        APC-->>A: Toastr "Payout approved & settled"
    else Admin Rejects Payout
        A->>APC: POST /Admin/Payouts/Reject (transactionId, reason)
        APC->>WS: ProcessPayoutAsync(transactionId, approve: false, reason)
        WS->>UOW: Wallets.GetByIdAsync(transactionId)
        WS->>WS: Update Transaction Status = Rejected
        WS->>UOW: SaveChangesAsync()
        UOW->>DB: UPDATE WalletTransactions
        APC-->>A: Toastr "Payout request rejected"
    end
```
## 4. Activity Diagram: Shopping and Checkout, and Wallet Settlement

```mermaid
flowchart TD
    S((Start)) --> A["Browse / search products"]
    A --> B["Open product details"]
    B --> C{"In stock?"}
    C -- No --> A
    C -- Yes --> D["Add to cart"]
    D --> E{"Continue shopping?"}
    E -- Yes --> A
    E -- No --> F["Open cart, edit quantities, apply promo code"]
    F --> G{"Logged in?"}
    G -- No --> H["Login or register"] --> I
    G -- Yes --> I["Proceed to checkout, enter shipping address"]
    I --> J["System checks stock"]
    J --> K{"Stock sufficient?"}
    K -- No --> L["Show message, return to cart"] --> F
    K -- Yes --> M["Begin transaction: save order & details with VendorId, reduce stock"]
    M --> N{"Saved successfully?"}
    N -- No --> O["Rollback, show error"] --> F
    N -- Yes --> P["Redirect to Stripe"]
    P --> Q{"Payment successful?"}
    Q -- No --> R["Order stays Pending / user can retry or cancel"]
    Q -- Yes --> S1["Mark order Paid, clear cart"]
    S1 --> S2["Calculate net vendor earnings after platform commission"]
    S2 --> S3["Credit Vendor WalletBalance & insert Deposit WalletTransaction"]
    S3 --> T["Show order confirmation page"]
    R --> Z((End))
    T --> Z
```

## 5. State Diagram: Vendor Verification Status

```mermaid
stateDiagram-v2
    [*] --> Pending : Submit legal docs (ID, Commercial Register, Tax Card)
    Pending --> Approved : Admin verifies documents
    Pending --> Rejected : Admin rejects documents (provides reason)
    Rejected --> Pending : Vendor updates & resubmits documents
    Approved --> [*]
```

## 6. State Diagram: Order Status

```mermaid
stateDiagram-v2
    [*] --> Pending : Order placed
    Pending --> Paid : Payment succeeded
    Pending --> Cancelled : Payment failed or customer/admin cancels
    Paid --> Shipped : Admin ships
    Paid --> Cancelled : Admin cancels
    Shipped --> Delivered : Admin confirms delivery
    Delivered --> [*]
    Cancelled --> [*]
```

| From | To | Triggered by | Side effect |
|---|---|---|---|
| Pending | Paid | Verified Stripe payment | `PaymentIntentId` stored |
| Pending / Paid | Cancelled | Admin or failed payment | Product stock is restored in the same transaction |
| Paid | Shipped | Admin | None |
| Shipped | Delivered | Admin | None |

Transitions not in the table are rejected by `OrderService`.

## 7. State Diagram: Wallet Transaction & Payout Status

```mermaid
stateDiagram-v2
    [*] --> Pending : Vendor submits payout request
    Pending --> Completed : Admin approves payout & confirms bank transfer
    Pending --> Rejected : Admin rejects payout request
    Completed --> [*]
    Rejected --> [*]
```

| From | To | Triggered by | Side effect |
|---|---|---|---|
| [*] | Deposit (Completed) | Order payment confirmed | `Vendor.WalletBalance` credited immediately |
| [*] | Pending (Withdrawal) | Vendor requests payout |  Payout request queued for admin review |
| Pending | Completed | Admin approves payout | `Vendor.WalletBalance` debited by requested amount |
| Pending | Rejected | Admin rejects payout | Request marked rejected, wallet balance remains untouched |
