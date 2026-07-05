## ADDED Requirements

### Requirement: Other modules can auto-post GL entries tagged with their source
A `GLEntry` SHALL record its origin via `Source` (`Manual`, `SalesAutoPost`, `InventoryAutoPost`) and, when auto-posted, a `SourceReferenceId` pointing back to the triggering Sales (or future Inventory) entity. Auto-posted entries SHALL be created via `GLEntry.CreateDraft(...)` immediately followed by `.Post(...)` in the same handler — they are never left in `Draft` for manual review.

#### Scenario: An invoice issuance posts a balanced, already-Posted GL entry
- **WHEN** `PostInvoiceToGLHandler` handles `InvoiceIssuedEvent`
- **THEN** it creates a `GLEntry` with `Source = SalesAutoPost` and `SourceReferenceId = InvoiceId`, debiting Accounts Receivable and crediting Sales Revenue (and SST Payable, if taxed), and the entry's `Status` is `Posted`, not `Draft`

#### Scenario: A payment verification posts a balanced, already-Posted GL entry
- **WHEN** `PostPaymentToGLHandler` handles `PaymentVerifiedEvent`
- **THEN** it creates a `GLEntry` with `Source = SalesAutoPost` and `SourceReferenceId = PaymentId`, debiting Cash (or the payment's linked `BankAccount`) and crediting Accounts Receivable, and the entry's `Status` is `Posted`
