## ADDED Requirements

### Requirement: Credit notes post a reversing GL entry
`CreditNote.Create()` SHALL raise `CreditNoteAppliedEvent`, dispatched the same way `InvoiceIssuedEvent`/`PaymentVerifiedEvent` are. The handler SHALL post a `GLEntry` with `Source = SalesAutoPost` debiting Sales Revenue and crediting Accounts Receivable for the full credit note `Amount` (no SST Payable adjustment, since `CreditNote` carries no per-line tax breakdown), already `Posted`. It SHALL no-op if Accounting isn't licensed, matching invoice/payment posting.

#### Scenario: Applying a credit note posts a balanced reversing GL entry
- **WHEN** a credit note for RM 200 is created against an invoice
- **THEN** a `Posted` `GLEntry` appears debiting Sales Revenue RM 200 and crediting Accounts Receivable RM 200

### Requirement: An invoice can be exported as a PDF
`GET /api/v1/sales/invoices/{id}/pdf` (permission `Sales.Invoice.Read`) SHALL render the invoice (lines, subtotal, SST, total, amount paid, outstanding balance) as a downloadable PDF using the company's letterhead, matching the layout convention of the 4 Accounting report PDF exports.

#### Scenario: Downloading an invoice PDF returns a valid PDF file
- **WHEN** `GET /sales/invoices/{id}/pdf` is called for an existing invoice
- **THEN** the response body starts with the `%PDF-` header and its `Content-Type` is `application/pdf`

### Requirement: Creating an invoice warns, but never blocks, when it would exceed the customer's credit limit
`POST /api/v1/sales/invoices` SHALL always succeed regardless of the customer's `CreditLimit`. If `CreditLimit > 0` and the customer's outstanding balance across their `Issued`/`PartiallyPaid` invoices plus the new invoice's `TotalAmount` would exceed `CreditLimit`, the response's `meta.creditLimitWarning` SHALL contain a human-readable message; otherwise it SHALL be `null`. A `CreditLimit` of `0` or less SHALL be treated as "no limit configured" and SHALL never produce a warning.

#### Scenario: An invoice that pushes a customer over their credit limit still succeeds, with a warning
- **WHEN** a customer with `CreditLimit = 1000` and RM 800 already outstanding is billed a new RM 300 invoice
- **THEN** the invoice is created (201) and `meta.creditLimitWarning` describes the RM 1,100 projected outstanding exceeding the RM 1,000 limit

#### Scenario: A customer with no credit limit configured never triggers a warning
- **WHEN** a customer with `CreditLimit = 0` is billed any invoice amount
- **THEN** `meta.creditLimitWarning` is always `null`

### Requirement: Invoice numbering stays unique under concurrent issuance
`Invoice.Issue()`'s number assignment, backed by the `InvoiceNumberSeq` SQL Server sequence, SHALL produce a unique number for every invoice even when many `Issue` requests are submitted concurrently.

#### Scenario: 20 invoices issued concurrently all receive distinct numbers
- **WHEN** 20 Draft invoices for the same customer are issued via concurrent `POST .../issue` requests
- **THEN** all 20 returned `InvoiceNumber` values are distinct
