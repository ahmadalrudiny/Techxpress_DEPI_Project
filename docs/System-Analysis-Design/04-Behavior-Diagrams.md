# TechXpress: Data Flow & System Behavior

## 1. DFD Level 0 (Context Diagram)

```mermaid
flowchart LR
    CUS["Customer"]
    ADM["Admin"]
    STR["Stripe"]
    SYS(("TechXpress<br/>System"))

    CUS -->|registration, login, search, cart actions, order details| SYS
    SYS -->|products, cart, order confirmation, order history| CUS
    ADM -->|product, category, order management| SYS
    SYS -->|reports of products, categories, orders| ADM
    SYS -->|payment session request| STR
    STR -->|payment result| SYS
```

## 2. DFD Level 1

```mermaid
flowchart LR
    CUS["Customer"]
    ADM["Admin"]
    STR["Stripe"]

    P1(("1.0<br/>Authentication"))
    P2(("2.0<br/>Browse Catalog"))
    P3(("3.0<br/>Manage Cart"))
    P4(("4.0<br/>Place Order"))
    P5(("5.0<br/>Process Payment"))
    P6(("6.0<br/>Manage Store - Admin"))
    P7(("7.0<br/>Manage Profile"))

    D1[("D1 Users and Roles")]
    D2[("D2 Products and Categories")]
    D3[("D3 Orders and OrderDetails")]
    D4[("D4 Session Cart")]

    CUS -->|credentials| P1
    P1 <-->|verify, create| D1
    P1 -->|auth result| CUS
    ADM -->|credentials| P1

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
    P5 -->|confirmation| CUS

    ADM -->|CRUD products, categories| P6
    ADM -->|change order status| P6
    P6 <-->|read, write| D2
    P6 <-->|read, write| D3

    CUS -->|update details| P7
    P7 <-->|read, write| D1
    P7 -->|read order history| D3
    P7 -->|profile and history| CUS
```

## 3. Sequence Diagrams

### 3.1 Login

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

### 3.2 Add to Cart

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

### 3.3 Checkout and Payment

```mermaid
sequenceDiagram
    actor C as Customer
    participant OC as CheckoutController
    participant OS as OrderService
    participant UOW as UnitOfWork
    participant PS as PaymentService
    participant ST as Stripe
    participant DB as Database

    C->>OC: POST /Checkout (shipping address)
    OC->>OS: PlaceOrderAsync(userId, cart, address)
    OS->>UOW: BeginTransactionAsync()
    loop each cart item
        OS->>UOW: Products.GetByIdAsync(productId)
        UOW->>DB: SELECT product
        OS->>OS: Check stock, reduce StockQuantity
    end
    OS->>UOW: Orders.AddAsync(order with details)
    OS->>UOW: CommitAsync()
    UOW->>DB: INSERT order, UPDATE stock (single transaction)
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
        ST-->>PS: Payment status
        PS->>OS: Mark order Paid, store PaymentIntentId
        OS->>UOW: Update order, SaveChangesAsync()
        OC-->>C: Order confirmation page, cart cleared
    end
```

### 3.4 Admin: Update Order Status

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

## 4. Activity Diagram: Shopping and Checkout

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
    K -- Yes --> M["Begin transaction: save order and reduce stock"]
    M --> N{"Saved successfully?"}
    N -- No --> O["Rollback, show error"] --> F
    N -- Yes --> P["Redirect to Stripe"]
    P --> Q{"Payment successful?"}
    Q -- No --> R["Order stays Pending / user can retry or cancel"]
    Q -- Yes --> T["Mark order Paid, clear cart, show confirmation"]
    R --> Z((End))
    T --> Z
```

## 5. State Diagram: Order Status

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
