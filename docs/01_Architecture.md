# 01 — Architecture Design

## 1.1 System Architecture (High Level)

```
                              ┌─────────────────────────┐
                              │   Browser (React SPA)    │
                              │   Vite build, served     │
                              │   from same host as API  │
                              └────────────┬─────────────┘
                                           │ HTTPS (JWT bearer)
                              ┌────────────▼─────────────┐
                              │   ASP.NET Core 9 Web API │
                              │   (Presentation Layer)    │
                              │   Controllers, Middleware │
                              └────────────┬─────────────┘
                                           │ MediatR (ICommand/IQuery)
                              ┌────────────▼─────────────┐
                              │    Application Layer      │
                              │  Handlers, DTOs, Validators│
                              │  (FluentValidation, AutoMapper)│
                              └────────────┬─────────────┘
                                           │
                              ┌────────────▼─────────────┐
                              │      Domain Layer         │
                              │  Entities, Value Objects,  │
                              │  Domain Services, Rules    │
                              │  (no external dependencies)│
                              └────────────┬─────────────┘
                                           │
                              ┌────────────▼─────────────┐
                              │   Infrastructure Layer    │
                              │  EF Core DbContext,        │
                              │  Repositories, Identity,   │
                              │  Serilog sinks             │
                              └────────────┬─────────────┘
                                           │
                              ┌────────────▼─────────────┐
                              │      SQL Server           │
                              └───────────────────────────┘
```

Single deployable backend, single deployable frontend bundle. No message queue, no microservices — this is a stand-alone product installed per-customer, not a multi-tenant SaaS. Simplicity here is deliberate: it must be operable by a customer's own IT staff or a small managed-hosting team.

## 1.2 Clean Architecture — Layer Responsibilities

| Layer | Project | Responsibility | Depends On |
|---|---|---|---|
| Presentation | `LitXus.Api` | Controllers, request/response models, auth middleware, Swagger config, global exception handler | Application |
| Application | `LitXus.Application` | MediatR commands/queries + handlers, DTOs, FluentValidation validators, AutoMapper profiles, `IFeatureFlagService`, `ICurrentUserService` | Domain |
| Domain | `LitXus.Domain` | Entities (GLEntry, Invoice, Product...), enums, domain exceptions, business-rule methods on entities (e.g. `Invoice.CanBeVoided()`) | Nothing (no NuGet deps beyond BCL) |
| Infrastructure | `LitXus.Infrastructure` | `AppDbContext` (EF Core), repository implementations, Identity setup, audit interceptor, Serilog sinks, external integrations (e.g. future MyInvois API client) | Application (implements its interfaces), Domain |

Dependency rule: **inner layers never reference outer layers.** Domain has zero project references. Application references Domain only and defines interfaces (`IGLRepository`, `IUnitOfWork`) that Infrastructure implements. This is what keeps the domain testable without a database.

## 1.3 Module Structure

Each business module (Accounting, Sales, Inventory) is a vertical slice inside Application/Domain/Infrastructure — not a separate project. Folder-per-module keeps Clean Architecture's project count small (4 projects total, not 12+):

```
LitXus.Domain/
  Modules/
    Accounting/   (Account, GLEntry, GLEntryLine, TaxCode, BankAccount, BankReconciliation)
    Sales/        (Customer, Invoice, InvoiceLine, Payment, CreditNote)
    Inventory/    (Product, StockLevel, StockMovement, Warehouse)
    Shared/       (AuditLog, User-related read models used across modules)

LitXus.Application/
  Modules/
    Accounting/Commands|Queries|Validators|Mappings
    Sales/Commands|Queries|Validators|Mappings
    Inventory/Commands|Queries|Validators|Mappings
    Identity/Commands|Queries (login, register, refresh, users, roles)
```

A module is "disabled" for a given install purely via the `FeatureFlag` check inside each controller/handler (see [16_Feature_Flags.md](16_Feature_Flags.md)) — the code always exists in the binary; it's gated, not compiled out. This avoids maintaining three separate builds.

## 1.4 Integration Points (Enterprise Pro only)

GL auto-posting is implemented as a **domain event → MediatR notification → handler** pattern, so Accounting stays decoupled from Sales/Inventory at compile time:

```
Sales.InvoicePosted (domain event)
      │
      ▼
MediatR INotification: InvoicePostedEvent
      │
      ▼
Accounting.Handlers.PostInvoiceToGLHandler
  (only registered/active if Accounting + Sales flags are both on)
      │
      ▼
Creates GLEntry (Dr Accounts Receivable / Cr Sales Revenue, Cr SST Payable)
```

Same pattern for `StockMovementRecordedEvent` → COGS GL posting. If a customer only licenses Retail Pro (no Accounting), the notification handler is simply never registered in DI — Sales/Inventory function identically with or without GL posting wired up.

## 1.5 Data Flow — Example (Sales Invoice → GL)

```
1. User submits invoice via React form (React Hook Form + Axios POST /api/sales/invoices)
2. InvoicesController → MediatR Send(CreateInvoiceCommand)
3. CreateInvoiceCommandHandler:
     - FluentValidation validator runs first (pipeline behavior)
     - Loads Customer, Product entities via repositories
     - Invoice.Create(...) domain factory enforces business rules
       (sequential invoice number, no negative qty, SST calc)
     - SaveChanges → AuditInterceptor captures before/after → AuditLog row written
     - Publishes InvoicePostedEvent
4. (Enterprise Pro only) PostInvoiceToGLHandler creates balanced GLEntry
5. Response DTO mapped via AutoMapper → 201 Created returned
6. Frontend: toast success, invoice list re-fetched (React Query-style refetch via Zustand action)
```

## 1.6 How Three Products Share One Codebase

- **One git repo, one build** for backend and frontend.
- **`Licenses` table** (see schema) stores which modules + expiry a given installed instance is entitled to.
- **`IFeatureFlagService.IsEnabled(Module.Accounting)`** is checked at three points:
  1. API — `[RequireModule(Module.Accounting)]` action filter returns 403 if disabled.
  2. Application — command/query handlers short-circuit (defense in depth, not just UI-trust).
  3. Frontend — route guards hide entire nav sections; `useFeatureFlags()` Zustand selector.
- Distribution: same installer/Docker image for all three products. What differs is the `appsettings.Production.json` → `Licensing:EnabledModules` value (or, longer-term, a signed license file). At install time, the customer's purchased product determines this config, not a different codebase.

## 1.7 Technology Integration Summary

| Concern | Technology | Notes |
|---|---|---|
| CQRS | MediatR | One handler per command/query; pipeline behaviors for validation, logging, transaction wrapping |
| ORM | EF Core 9 | Code-First, migrations per module folder |
| Validation | FluentValidation | Validators auto-discovered, run as MediatR pipeline behavior before handler executes |
| Mapping | AutoMapper | Entity ↔ DTO profiles per module |
| Auth | ASP.NET Identity + JWT | Access token (short-lived) + refresh token (rotated, stored hashed) |
| API docs | Swashbuckle (Swagger/OpenAPI 3.0) | Auto-generated from XML doc comments + DTOs |
| Logging | Serilog | Console + rolling file sink locally; structured JSON sink for cloud/demo |
