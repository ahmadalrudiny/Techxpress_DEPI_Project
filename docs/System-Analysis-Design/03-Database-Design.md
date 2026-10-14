# TechXpress: Database Design & Data Modeling

## 1. ER Diagram

```mermaid
erDiagram
    ASPNETUSERS ||--o{ ORDERS : places
    ASPNETUSERS ||--o| VENDORS : "has vendor profile"
    ASPNETUSERS }o--o{ ASPNETROLES : "has (AspNetUserRoles)"
    CATEGORIES ||--o{ PRODUCTS : contains
    ORDERS ||--|{ ORDERDETAILS : includes
    PRODUCTS ||--o{ ORDERDETAILS : "appears in"
    VENDORS ||--o{ PRODUCTS : "lists and sells"
    VENDORS ||--o{ ORDERDETAILS : "supplies"
    VENDORS ||--o{ WALLETTRANSACTIONS : "has transactions"

    ASPNETUSERS {
        string Id PK
        string UserName
        string Email
        string PasswordHash
        string PhoneNumber
        string FullName
        string Address
    }
    ASPNETROLES {
        string Id PK
        string Name "Admin, Customer, or Vendor"
    }
    VENDORS {
        int Id PK
        string UserId FK
        string StoreName
        string NationalIdUrl
        string CommercialRegisterUrl
        string TaxCardUrl
        int VerificationStatus "0 Pending, 1 Approved, 2 Rejected"
        string RejectionReason
        datetime SubmittedAt
        decimal WalletBalance
    }
    CATEGORIES {
        int Id PK
        string Name
        string Description
    }
    PRODUCTS {
        int Id PK
        string Name
        string Description
        decimal Price
        int StockQuantity
        string ImageUrl
        bool IsActive
        datetime CreatedAt
        int CategoryId FK
        int VendorId FK
    }
    ORDERS {
        int Id PK
        string UserId FK
        datetime OrderDate
        decimal TotalAmount
        decimal DiscountAmount
        string PromoCode
        int Status "enum OrderStatus"
        string PaymentIntentId
        string ShippingAddress
    }
    ORDERDETAILS {
        int Id PK
        int OrderId FK
        int ProductId FK
        int VendorId FK
        int Quantity
        decimal UnitPrice
    }
    WALLETTRANSACTIONS {
        int Id PK
        int VendorId FK
        decimal Amount
        string Type "Deposit, Withdrawal"
        string Status "Pending, Completed, Rejected"
        string Description
        datetime CreatedAt
    }
```

> `DiscountAmount` and `PromoCode` support the promotions requirement (FR-17).WalletBalance and WalletTransactions implement the internal ledger wallet system for vendor earnings and platform commissions.

## 2. Logical Schema (Relational Model)

- **Categories**(<u>Id</u>, Name, Description)
- **Products**(<u>Id</u>, Name, Description, Price, StockQuantity, ImageUrl, IsActive, CreatedAt, CategoryId, VendorId)
- **AspNetUsers**(<u>Id</u>, UserName, Email, PasswordHash, PhoneNumber, FullName, Address, ...)
- **AspNetRoles**(<u>Id</u>, Name)
- **AspNetUserRoles**(<u>*UserId*, *RoleId*</u>)
- **Vendors**(<u>Id</u>, UserId, StoreName, NationalIdUrl, CommercialRegisterUrl, TaxCardUrl, VerificationStatus, RejectionReason, SubmittedAt, WalletBalance)
- **Orders**(<u>Id</u>, *UserId*, OrderDate, TotalAmount, DiscountAmount, PromoCode, Status, PaymentIntentId, ShippingAddress)
- **OrderDetails**(<u>Id</u>, *OrderId*, *ProductId*, VendorId, Quantity, UnitPrice)
- **WalletTransactions**(<u>Id</u>, *VendorId*, Amount, Type, Status,Description, CreatedAt)

(Underlined = primary key, italic = foreign key.)

## 3. Physical Schema (SQL Server)

### Vendors
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| UserId | nvarchar(100) | FK to AspNetUsers(Id), NOT NULL |
| StoreName | nvarchar(150) | NULL |
| NationalIdUrl | nvarchar(max) |NOT NULL |
| CommercialRegisterUrl | nvarchar(max) |NOT NULL |
| TaxCardUrl | nvarchar(max) | NOT NULL |
| VerificationStatus | int | NOT NULL (0 = Pending, 1 = Approved, 2 = Rejected) |
| RejectionReason | nvarchar(max) | NULL |
| SubmittedAt | datetime2 | NOT NULL |
| WalletBalance | decimal(18,2) | NOT NULL, default 0.00 |


### Categories
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| Name | nvarchar(100) | NOT NULL |
| Description | nvarchar(max) | NULL |

### Products
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| Name | nvarchar(200) | NOT NULL |
| Description | nvarchar(max) | NULL |
| Price | decimal(18,2) | NOT NULL |
| StockQuantity | int | NOT NULL |
| ImageUrl | nvarchar(max) | NULL |
| IsActive | bit | NOT NULL, default 1 |
| CreatedAt | datetime2 | NOT NULL |
| CategoryId | int | FK to Categories(Id), ON DELETE RESTRICT |
| VendorId | int | FK to Vendors(Id), NULLable (for system/admin products) |

### Orders
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| UserId | nvarchar(450) | FK to AspNetUsers(Id), NOT NULL |
| OrderDate | datetime2 | NOT NULL |
| TotalAmount | decimal(18,2) | NOT NULL |
| DiscountAmount | decimal(18,2) | NOT NULL, default 0 |
| PromoCode | nvarchar(50) | NULL |
| Status | int | NOT NULL (0 Pending, 1 Paid, 2 Shipped, 3 Delivered, 4 Cancelled) |
| PaymentIntentId | nvarchar(max) | NULL |
| ShippingAddress | nvarchar(max) | NULL |

### OrderDetails
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| OrderId | int | FK to Orders(Id), ON DELETE CASCADE |
| ProductId | int | FK to Products(Id), ON DELETE RESTRICT |
| VendorId | int | FK to Vendors(Id), NULLable |
| Quantity | int | NOT NULL |
| UnitPrice | decimal(18,2) | NOT NULL |

### WalletTransactions
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, identity |
| VendorId | int | FK to Vendors(Id), ON DELETE RESTRICT |
| Amount | decimal(18,2) | NOT NULL |
| Type | nvarchar(50) | NOT NULL (Deposit, Withdrawal) |
| Status | nvarchar(50) | NOT NULL (Pending, Completed, Rejected) |
| Description | nvarchar(250) | NULL |
| CreatedAt | datetime2 | NOT NULL, default GETUTCDATE() |

### Identity tables
`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens` are generated by ASP.NET Core Identity. `AspNetUsers` is extended with `FullName` and `Address`.

## 4. Normalization Notes
- All tables are in **3NF**: every non-key attribute depends on the key only.
- `OrderDetails.UnitPrice` intentionally **duplicates** the product price at purchase time so old orders do not change when the price changes (a controlled denormalization).
- `OrderDetails.VendorId` explicitly records the supplying vendor at the moment of order placement to preserve historical revenue attribution even if the product entity is modified or soft-deleted later.
- `Vendors.WalletBalance` and **WalletTransactions** maintain an internal audit ledger for financial calculations, platform commission deductions, and payout request processing.
- The many-to-many relation between users and roles is resolved by `AspNetUserRoles`.
- Delete rules protect history: a category with products and a product with past orders cannot be deleted (use `IsActive = false` instead).

## 5. Class Diagram

```mermaid
classDiagram
    direction LR

    class ApplicationUser {
        +string FullName
        +string Address
    }
    class Vendor {
        +int Id
        +string UserId
        +string StoreName
        +string NationalIdUrl
        +string CommercialRegisterUrl
        +string TaxCardUrl
        +VerificationStatus Status
        +string RejectionReason
        +DateTime SubmittedAt
        +decimal WalletBalance
    }
    class Category {
        +int Id
        +string Name
        +string Description
    }
    class Product {
        +int Id
        +string Name
        +string Description
        +decimal Price
        +int StockQuantity
        +string ImageUrl
        +bool IsActive
        +DateTime CreatedAt
        +int CategoryId
        +int? VendorId
    }
    class Order {
        +int Id
        +DateTime OrderDate
        +decimal TotalAmount
        +decimal DiscountAmount
        +string PromoCode
        +OrderStatus Status
        +string PaymentIntentId
        +string ShippingAddress
    }
    class OrderDetail {
        +int Id
        +int OrderId
        +int ProductId
        +int? VendorId
        +int Quantity
        +decimal UnitPrice
    }
    class WalletTransaction {
        +int Id
        +int VendorId
        +decimal Amount
        +string Type
        +string Status
        +string Description
        +DateTime CreatedAt
    }
    class OrderStatus {
        <<enumeration>>
        Pending
        Paid
        Shipped
        Delivered
        Cancelled
    }
    class VerificationStatus {
        <<enumeration>>
        Pending
        Approved
        Rejected
    }

    ApplicationUser "1" --> "0..1" Vendor
    ApplicationUser "1" --> "*" Order
    Vendor "1" --> "*" Product
    Vendor "1" --> "*" WalletTransaction
    Vendor "1" --> "*" OrderDetail
    Category "1" --> "*" Product
    Order "1" --> "*" OrderDetail
    Product "1" --> "*" OrderDetail
    Order --> OrderStatus
    Vendor --> VerificationStatus

    class IGenericRepository~T~ {
        <<interface>>
        +GetByIdAsync(int id)
        +GetAllAsync()
        +FindAsync(predicate)
        +Query() IQueryable
        +AddAsync(T entity)
        +Update(T entity)
        +Delete(T entity)
    }
    class GenericRepository~T~
    class IProductRepository {
        <<interface>>
        +GetAllWithCategoryAsync()
        +GetByIdWithCategoryAsync(int id)
        +GetByCategoryAsync(int categoryId)
    }
    class ICategoryRepository {
        <<interface>>
        +GetAllWithProductsAsync()
    }
    class IVendorRepository {
        <<interface>>
        +GetPendingVendorsAsync()
        +GetByUserIdAsync(string userId)
    }
    class IWalletRepository {
        <<interface>>
        +GetTransactionsByVendorAsync(int vendorId)
        +GetPendingPayoutsAsync()
    }
    class ProductRepository
    class CategoryRepository
    class VendorRepository
    class WalletRepository
    class IUnitOfWork {
        <<interface>>
        +IProductRepository Products
        +ICategoryRepository Categories
        +IVendorRepository Vendors
        +IWalletRepository Wallets
        +IGenericRepository~Order~ Orders
        +IGenericRepository~OrderDetail~ OrderDetails
        +SaveChangesAsync()
        +BeginTransactionAsync()
        +CommitAsync()
        +RollbackAsync()
    }
    class UnitOfWork
    class ApplicationDbContext

    IGenericRepository~T~ <|.. GenericRepository~T~
    IGenericRepository~T~ <|-- IProductRepository
    IGenericRepository~T~ <|-- ICategoryRepository
    IGenericRepository~T~ <|-- IVendorRepository
    IGenericRepository~T~ <|-- IWalletRepository
    GenericRepository~T~ <|-- ProductRepository
    GenericRepository~T~ <|-- CategoryRepository
    GenericRepository~T~ <|-- VendorRepository
    GenericRepository~T~ <|-- WalletRepository
    IProductRepository <|.. ProductRepository
    ICategoryRepository <|.. CategoryRepository
    IVendorRepository <|.. VendorRepository
    IWalletRepository <|.. WalletRepository
    IUnitOfWork <|.. UnitOfWork
    UnitOfWork --> ApplicationDbContext
    GenericRepository~T~ --> ApplicationDbContext

    class IProductService {
        <<interface>>
        +GetProductsAsync(search, categoryId, page)
        +GetByIdAsync(id)
    }
    class IVendorService {
        <<interface>>
        +SubmitDocumentsAsync(vendorDto)
        +GetPendingVendorsAsync()
        +VerifyVendorAsync(vendorId, isApproved, reason)
    }
    class ICartService {
        <<interface>>
        +AddItem(productId, quantity)
        +UpdateQuantity(productId, quantity)
        +RemoveItem(productId)
        +ApplyPromoCode(code)
        +GetCart() CartViewModel
        +Clear()
    }
    class IOrderService {
        <<interface>>
        +PlaceOrderAsync(userId, cart, address)
        +GetUserOrdersAsync(userId)
        +GetOrderAsync(id, userId)
        +UpdateStatusAsync(id, status)
    }
    class IPaymentService {
        <<interface>>
        +CreateCheckoutSessionAsync(order)
        +ConfirmPaymentAsync(sessionId)
    }
    class IWalletService {
        <<interface>>
        +ProcessOrderCommissionAsync(orderId)
        +RequestPayoutAsync(vendorId, amount)
        +ProcessPayoutAsync(transactionId, approve)
    }
    class ProductService
    class VendorService
    class CartService
    class OrderService
    class PaymentService
    class WalletService

    IProductService <|.. ProductService
    IVendorService <|.. VendorService
    ICartService <|.. CartService
    IOrderService <|.. OrderService
    IPaymentService <|.. PaymentService
    IWalletService <|.. WalletService
    ProductService --> IUnitOfWork
    VendorService --> IUnitOfWork
    OrderService --> IUnitOfWork
    WalletService --> IUnitOfWork
```
