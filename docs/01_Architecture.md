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
                              │  (FluentValidation, hand-mapped DTOs)│
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
| Application | `LitXus.Application` | MediatR commands/queries + handlers, DTOs with hand-written `ToDto()` mapping extensions, FluentValidation validators, `IFeatureFlagService`, `ICurrentUserService` | Domain |
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

GL auto-posting is implemented as a **domain event → MediatR notification → handler** pattern, so Accounting stays decoupled from Sales/Inventory at compile time. Built in Phase 2 (Sales); the mechanism is generic and Phase 3 (Inventory) reuses it for COGS posting.

`BaseEntity` queues plain `IDomainEvent`s (a framework-free marker interface in `LitXus.Domain.Common` — Domain must never reference MediatR). A `DomainEventDispatchInterceptor` (mirrors the existing `AuditSaveChangesInterceptor`) collects and dispatches them via MediatR's `IPublisher` **after** `SaveChangesAsync` commits, not before — a failed transaction must never fire an event. Each event is wrapped in a generic `DomainEventNotification<TDomainEvent> : INotification` before publishing.

```
Invoice.Issue() domain method
      │  AddDomainEvent(new InvoiceIssuedEvent(Id))
      ▼
SaveChangesAsync commits
      │
      ▼
DomainEventDispatchInterceptor.SavedChangesAsync
      │  wraps in DomainEventNotification<InvoiceIssuedEvent>, publishes via IPublisher
      ▼
Accounting.EventHandlers.PostInvoiceToGLHandler
  (always registered via MediatR's assembly scan — checks
   IFeatureFlagService.IsEnabled(Module.Accounting) itself and no-ops if disabled)
      │
      ▼
Creates GLEntry (Dr Accounts Receivable / Cr Sales Revenue, Cr SST Payable), already Posted
```

Same pattern for `PaymentVerifiedEvent` → `PostPaymentToGLHandler` (Dr Cash/Bank / Cr Accounts Receivable). A later `StockMovementRecordedEvent` → COGS GL posting (Phase 3) is expected to follow the identical shape.

**Correction from the original design:** the handler is **always** registered in DI (MediatR's `RegisterServicesFromAssembly` auto-discovers every `INotificationHandler<T>` regardless of licensing) — it is not conditionally registered based on which modules are enabled. Instead, each handler's first line checks `IFeatureFlagService.IsEnabled(Module.Accounting)` and returns immediately if it's off. This was chosen over conditional DI registration because MediatR's auto-discovery makes "don't register this handler" awkward to express safely, and a licensed-check no-op is behaviorally identical: if a customer only licenses Retail Pro (no Accounting), `Invoice.Issue()`/`Payment.Verify()` still succeed identically, simply with no `GLEntry` created.

## 1.5 Data Flow — Example (Sales Invoice → GL)

```
1. User submits invoice via React form (React Hook Form + Axios POST /api/v1/sales/invoices)
2. InvoicesController → MediatR Send(CreateInvoiceCommand) → creates a Draft invoice
   (Phase 2 lines are free-text Description/Quantity/UnitPrice — no Product entity exists
   until Phase 3, so there's no Product lookup at this step yet)
3. A second request, POST /api/v1/sales/invoices/{id}/issue → IssueInvoiceCommandHandler:
     - Loads the Invoice, calls Invoice.Issue(invoiceNumber) — assigns the next sequential
       number via INumberSequenceGenerator, flips Draft -> Issued, raises InvoiceIssuedEvent
     - SaveChangesAsync → AuditSaveChangesInterceptor captures before/after (AuditLog row)
       AND DomainEventDispatchInterceptor dispatches InvoiceIssuedEvent after commit
4. (Only if Accounting is licensed) PostInvoiceToGLHandler creates a balanced, already-Posted
   GLEntry (Dr Accounts Receivable / Cr Sales Revenue, Cr SST Payable if taxed)
5. Response mapped to a DTO via the entity's `ToDto()` extension method → 200 OK returned
6. Frontend: invoice list re-fetched (plain fetch-and-setState in the Sales pages, not a
   Redux-Saga action — the Sales pages built in Phase 2 follow the same local-state pattern
   as Phase 1's Accounting pages, not the template's original Redux-Saga scaffolding)
```

## 1.6 How Three Products Share One Codebase

- **One git repo, one build** for backend and frontend.
- **`Licenses` table** (see schema) stores which modules + expiry a given installed instance is entitled to.
- **`IFeatureFlagService.IsEnabled(Module.Accounting)`** is checked at three points:
  1. API — `[RequireModule(Module.Accounting)]` action filter returns 403 if disabled.
  2. Application — command/query handlers short-circuit (defense in depth, not just UI-trust).
  3. Frontend — route guards (`PrivateRoute` + a module check) hide entire nav sections; `state.Auth.user.enabledModules` read via a `useSelector` in the Redux store the Konrix template already ships with.
- Distribution: same installer/Docker image for all three products. What differs is the `appsettings.Production.json` → `Licensing:EnabledModules` value (or, longer-term, a signed license file). At install time, the customer's purchased product determines this config, not a different codebase.

## 1.7 Technology Integration Summary

| Concern | Technology | Notes |
|---|---|---|
| CQRS | MediatR | One handler per command/query; pipeline behaviors for validation, logging, transaction wrapping |
| ORM | EF Core 10 | Code-First, migrations per module folder |
| Validation | FluentValidation | Validators auto-discovered, run as MediatR pipeline behavior before handler executes |
| Mapping | Hand-written `ToDto()` extensions | No AutoMapper — dropped for licensing + an unpatched CVE, see [00_Overview.md](00_Overview.md) |
| Auth | ASP.NET Identity + JWT | Access token (short-lived) + refresh token (rotated, stored hashed); frontend uses `Authorization: Bearer` (Konrix template default was `JWT` prefix — corrected to match ASP.NET Core's JwtBearer scheme) |
| API docs | Swashbuckle (Swagger/OpenAPI 3.0) | Auto-generated from XML doc comments + DTOs |
| Logging | Serilog | Console + rolling file sink locally; structured JSON sink for cloud/demo |
| Frontend state | Redux Toolkit + Redux-Saga | Konrix template's own architecture — one slice per domain (`Auth`, `Layout`, and new `Accounting`/`Sales`/`Inventory` slices added per phase), sagas handle async API calls |
| Frontend charts | ApexCharts (`react-apexcharts`) | Template's bundled charting library — used for Phase 5 dashboards/KPI widgets instead of Recharts |
