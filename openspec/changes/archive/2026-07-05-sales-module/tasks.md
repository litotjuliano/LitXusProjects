## 1. Domain Event Infrastructure

- [x] 1.1 `IDomainEvent` marker interface, `BaseEntity.AddDomainEvent`/`DomainEvents`/`ClearDomainEvents`
- [x] 1.2 `DomainEventNotification<TDomainEvent>` (Application), `DomainEventDispatchInterceptor` (Infrastructure, dispatches after `SavedChanges`/`SavedChangesAsync`)
- [x] 1.3 `InvoiceIssuedEvent`, `PaymentVerifiedEvent` (Domain), `PostInvoiceToGLHandler`, `PostPaymentToGLHandler` (Application, feature-flag-gated no-op)

## 2. Sales Domain Layer

- [x] 2.1 `Customer`, `Invoice`/`InvoiceLine`, `Payment`, `CreditNote`, `SalesSettings` entities + `SalesExceptions.cs`

## 3. Application CQRS

- [x] 3.1 Customer commands/queries
- [x] 3.2 Invoice commands/queries (create/update/issue/void, list/get)
- [x] 3.3 Payment commands/queries (record/verify/reject, list)
- [x] 3.4 Credit note commands/queries
- [x] 3.5 `ConfigureSalesSettings`/`GetSalesSettings`
- [x] 3.6 `GetSalesSummary`/`GetArAging`

## 4. Infrastructure

- [x] 4.1 EF configurations for all 6 new entities, soft-delete filters on Invoice/CreditNote
- [x] 4.2 `AddSalesModule` migration (6 tables, `InvoiceNumberSeq`/`CreditNoteNumberSeq` sequences)
- [x] 4.3 `NumberSequenceGenerator` extended (`NextInvoiceNumberAsync`/`NextCreditNoteNumberAsync`)
- [x] 4.4 `PermissionCatalog` extended with `Sales.*` permissions
- [x] 4.5 `RbacSeeder` extended with Sales role grants, **and fixed to seed additively** (was a one-shot "if any permissions exist, skip" guard that silently dropped Phase 2's new permissions/grants on an already-seeded database — see design.md)
- [x] 4.6 `SalesDemoDataSeeder` (settings, 41 customers, 24 invoices, 18 payments, 2 credit notes)

## 5. API Layer

- [x] 5.1 `CustomersController`, `InvoicesController`, `PaymentsController`, `CreditNotesController`, `SalesReportsController`, `SalesSettingsController` — all `[RequireModule(Module.Sales)]`

## 6. Frontend

- [x] 6.1 `helpers/api/sales.ts`
- [x] 6.2 `pages/sales/Customers.tsx`, `Invoices.tsx` (line editor + Issue/Void + payment recording), `Payments.tsx` (Verify/Reject queue), `CreditNotes.tsx`
- [x] 6.3 `pages/sales/reports/SalesSummary.tsx`, `ArAging.tsx` (plain tables, matching the existing Accounting reports convention)
- [x] 6.4 `pages/sales/SalesSettings.tsx` (Admin GL-account-mapping form)
- [x] 6.5 New routes (`routes/index.tsx`) and "Sales" menu section (`constants/menu.ts`, `module: 'Sales'`)

## 7. Licensing

- [x] 7.1 Regenerate the seeded dev license to include `Sales` (was `Accounting`-only) via `backend/tools/LitXus.LicenseGenerator`

## 8. Documentation

- [x] 8.1 OpenSpec `sales` capability (this change) + `accounting` delta (`SalesAutoPost` GL posting)
- [x] 8.2 `docs/phase-2-sales/` doc set (`Features.md`, `Business_Rules.md`, `API_Specification.md`, `Database_Schema.md`, `UI_Mockups.md`, `Sample_Data.md`, `Test_Scenarios.md`, `User_Guide.md` with real screenshots)
- [x] 8.3 Root doc updates: `docs/03_API_Specification.md` §3.6, `docs/05_Phase_Breakdown.md` Phase 2 checklist, `docs/08_Sample_Data.md` (also corrected the now-stale `RbacSeeder` idempotency description), `docs/00_Overview.md`, `docs/01_Architecture.md` §1.4/§1.5 (corrected to match the actual always-registered-handler-with-internal-flag-check implementation, not conditional DI registration)

## 9. Tests

- [x] 9.1 Domain unit tests: `InvoiceTests` (21 cases: draft totals, SST inclusion, issue/void/apply-payment lifecycle, overdue), `PaymentTests` (verify/reject lifecycle), `CreditNoteTests`
- [x] 9.2 `InvoiceToGLPostingTests` — Application/integration test proving the domain-event dispatch actually fires a real GL posting end-to-end (create accounts → configure SalesSettings → create customer → create + issue invoice → confirm a Posted `GLEntry` with matching Dr/Cr lines exists)

## 10. Verification

- [x] 10.1 Backend + frontend build clean (`dotnet build`, `npx tsc --noEmit`, `npm run build`)
- [x] 10.2 Live (Playwright + direct API calls): login → Sales Settings shows configured accounts → issue a Draft invoice → GL entry appears with `Source = SalesAutoPost` → record + verify a payment → invoice flips to Paid, second GL entry posts → Credit Notes/Sales Summary/AR Aging all render
- [x] 10.3 Confirm `SalesUser` gets 403 verifying a payment and voiding/issuing an invoice, 201 creating a customer
- [x] 10.4 `dotnet test` — full suite passes including new Sales tests (90/90: 77 unit + 13 integration)
