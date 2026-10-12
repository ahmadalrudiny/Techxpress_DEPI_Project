# TechXpress: Problem Statement, Architecture & Deployment

## 1. Problem Statement
Small and medium electronics sellers often rely on social media or manual processes to sell products. This leads to unmanaged stock, lost or duplicated orders, no payment tracking, and no central place to manage the catalog. Customers cannot easily browse, compare, and pay online.

## 2. Objectives
1. Provide an online store where customers can browse, search, and filter electronics.
2. Let customers manage a shopping cart and place orders with secure online payment (Stripe).
3. Guarantee data consistency: an order and its stock reduction are saved atomically.
4. Give administrators a protected panel to manage products, categories, and orders.
5. Apply N-Tier Architecture, Repository, Unit of Work, and Dependency Injection for a maintainable code base.
6. Deploy the system on Microsoft Azure.

## 3. Software Architecture (N-Tier, MVC)

```mermaid
flowchart TB
    U["User Browser<br/>Razor Views, Bootstrap, jQuery, DataTables, Toastr"]

    subgraph WEB["TechXpress.Web - Presentation Layer"]
        MW["Middleware<br/>HTTPS, Session, Authentication, Authorization"]
        C["Controllers and Areas/Admin"]
        V["Views and ViewModels"]
    end

    subgraph SVC["TechXpress.Services - Business Logic Layer"]
        PS["ProductService"]
        CS["CategoryService"]
        CART["CartService"]
        OS["OrderService"]
        PAY["PaymentService"]
    end

    subgraph DATA["TechXpress.Data - Data Access Layer"]
        UOW["UnitOfWork"]
        REPO["Generic, Product and Category Repositories"]
        CTX["ApplicationDbContext - EF Core"]
    end

    DB[("SQL Server / Azure SQL")]
    STRIPE["Stripe API"]

    U -->|HTTP request| MW --> C
    C --> V
    C --> PS & CS & CART & OS & PAY
    PS & CS & OS --> UOW
    UOW --> REPO --> CTX --> DB
    PAY -->|create session / verify payment| STRIPE
```

**Request flow:** `Controller -> Service -> UnitOfWork -> Repository -> DbContext -> SQL Server`.
Controllers never access the database directly; each layer only knows the layer below it.

| Layer | Project | Responsibility |
|---|---|---|
| Presentation | `TechXpress.Web` | Controllers, Views, ViewModels, client-side scripts, authentication middleware |
| Business Logic | `TechXpress.Services` | Business rules: cart totals, discounts, stock checks, order placement, payment orchestration |
| Data Access | `TechXpress.Data` | Entities, `DbContext`, migrations, repositories, Unit of Work, seed data |

## 4. Component Diagram

```mermaid
flowchart LR
    subgraph Presentation
        HC["HomeController"]
        PC["ProductsController"]
        AC["AccountController"]
        CC["CartController"]
        OC["CheckoutController"]
        PRC["ProfileController"]
        ADM["Admin Area Controllers"]
    end

    subgraph Business
        IPS["IProductService"]
        ICS["ICategoryService"]
        ICART["ICartService"]
        IOS["IOrderService"]
        IPAY["IPaymentService"]
    end

    subgraph Data
        IUOW["IUnitOfWork"]
        IPR["IProductRepository"]
        ICR["ICategoryRepository"]
        IGR["IGenericRepository of T"]
        CTX["ApplicationDbContext"]
    end

    subgraph External
        IDN["ASP.NET Core Identity"]
        SES["Session State"]
        STR["Stripe"]
        SQL[("SQL Server")]
    end

    PC --> IPS
    HC --> IPS
    PC --> ICS
    CC --> ICART
    OC --> IOS & IPAY
    PRC --> IOS
    ADM --> IPS & ICS & IOS
    AC --> IDN
    ICART --> SES
    IPS & ICS & IOS --> IUOW
    IUOW --> IPR & ICR & IGR
    IPR & ICR & IGR --> CTX --> SQL
    IPAY --> STR
    IDN --> CTX
```

## 5. Deployment Diagram

```mermaid
flowchart LR
    subgraph Client["Client Device"]
        BR["Web Browser<br/>(Desktop, Tablet, Mobile)"]
    end

    subgraph Azure["Microsoft Azure"]
        subgraph APP["App Service (Windows/Linux, .NET 8)"]
            WEBAPP["TechXpress ASP.NET Core MVC App<br/>(Web + Services + Data assemblies)"]
            IMG["wwwroot/images<br/>(product images)"]
            ENV["Application Settings<br/>(connection string, Stripe keys)"]
        end
        SQL[("Azure SQL Database<br/>TechXpressDb")]
    end

    subgraph Ext["External Services"]
        STRIPE["Stripe Payments"]
    end

    GH["GitHub Repository<br/>main branch"]

    BR -->|HTTPS| WEBAPP
    WEBAPP -->|EF Core / TCP 1433| SQL
    WEBAPP -->|HTTPS API| STRIPE
    STRIPE -.->|redirect back| BR
    GH -->|manual publish or GitHub Actions| APP
```

## 6. Technology Stack

| Area | Technology |
|---|---|
| Backend | C#, ASP.NET Core MVC (.NET 8) |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity |
| Database | SQL Server (LocalDB / SQL Express for development, Azure SQL for production) |
| Frontend | Razor Views, Bootstrap, custom CSS, jQuery, DataTables, Toastr JS |
| Payment | Stripe |
| Cloud | Microsoft Azure (App Service + Azure SQL) |
| Tools | Visual Studio 2022, Git, GitHub |
| Testing | xUnit, Moq, EF Core InMemory/SQLite for repository tests |
