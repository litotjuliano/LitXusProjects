## Why

Phase 2 (`docs/05_Phase_Breakdown.md`) is the Sales module: Customers, Invoices (Draft→Issued→PartiallyPaid/Paid/Void, with a computed Overdue state), Payments (recorded, then Admin-verified), Credit Notes, and 2 Sales reports. None of this existed before this change — no Sales domain entities, permissions, controllers, or frontend pages. This also introduces the first cross-module integration in the codebase: Sales must post to the General Ledger without a compile-time dependency on Accounting, so that Sales works standalone in a deployment licensed for Sales only.

## What Changes

- New Sales domain layer: `Customer`, `Invoice`/`InvoiceLine`, `Payment`, `CreditNote`, `SalesSettings`.
- New domain-event infrastructure (`IDomainEvent`, `BaseEntity.AddDomainEvent`, `DomainEventDispatchInterceptor`, `DomainEventNotification<T>`) — `Invoice.Issue()` raises `InvoiceIssuedEvent`, `Payment.Verify()` raises `PaymentVerifiedEvent`, dispatched via MediatR *after* the triggering `SaveChangesAsync` commits.
- New Accounting-side event handlers (`PostInvoiceToGLHandler`, `PostPaymentToGLHandler`) that auto-post GL entries for invoice issuance and payment verification, no-op when Accounting isn't licensed (checked via `IFeatureFlagService`, not conditional DI registration).
- New `SalesSettings` singleton entity holding the 4 default GL account mappings an Admin configures once.
- Full Sales CQRS (customers, invoices, payments, credit notes, 2 reports), 6 new controllers, 6 new frontend pages + 2 report pages + a Sales Settings admin page, new nav section gated by the `Sales` license module.
- New sequential document numbering (`INV-{year}-{seq:D6}`, `CN-{year}-{seq:D6}`) via new SQL Server sequences.
- New RBAC permissions (`Sales.*`) and role grants matching `docs/06_RBAC_Auth.md` §6.2.
- Fixed `RbacSeeder` to seed additively (new permissions/grants now reach an already-seeded database) — see design.md.
- New `SalesDemoDataSeeder`: 41 customers, 24 invoices across 6 months (2 Draft, 22 Issued via real domain calls), 18 payments (15 Verified, 1 Rejected, 2 Pending), 2 credit notes.

## Capabilities

### New Capabilities
- `sales`: Customers, Invoices, Payments, Credit Notes, Sales Settings, Sales Reports, and the cross-module GL auto-posting contract.

### Modified Capabilities
- `accounting`: adds the Sales-triggered auto-posting requirements (`GLEntrySource.SalesAutoPost`) — the accounting domain itself (`Account`, `GLEntry`) is unchanged; only new *callers* of `GLEntry.CreateDraft`/`.Post` exist now, in the new event handlers.

## Impact

- Backend: `LitXus.Domain.Modules.Sales.*`, `LitXus.Domain.Common.IDomainEvent`, `LitXus.Application.Common.Events.DomainEventNotification<T>`, `LitXus.Application.Modules.Sales.*`, `LitXus.Application.Modules.Accounting.EventHandlers.{PostInvoiceToGLHandler,PostPaymentToGLHandler}`, `LitXus.Infrastructure.Persistence.Interceptors.DomainEventDispatchInterceptor`, 6 new EF configurations + `AddSalesModule` migration, `NumberSequenceGenerator` extended, `PermissionCatalog`/`RbacSeeder` extended (and `RbacSeeder` made additive), `SalesDemoDataSeeder`, 6 new API controllers.
- Frontend: `helpers/api/sales.ts`, `pages/sales/{Customers,Invoices,Payments,CreditNotes,SalesSettings}.tsx`, `pages/sales/reports/{SalesSummary,ArAging}.tsx`, new routes + menu section.
- Licensing: dev license regenerated to include the `Sales` module (was `Accounting`-only).
- No changes to existing Accounting domain entities or endpoints — purely additive.
