## ADDED Requirements

### Requirement: Customers can be created, updated, and deactivated but never deleted
`Customer` SHALL have an immutable `Code` (set only at creation) plus `CompanyName`, `ContactPerson`, `Email`, `Phone`, `Address`, `CreditLimit`, `PaymentTermsDays`, and `IsActive`. Deactivation SHALL be the only lifecycle action — there SHALL be no delete operation.

#### Scenario: Code cannot be changed on update
- **WHEN** `PUT /api/v1/sales/customers/{id}` is called (permission `Sales.Customer.Update`)
- **THEN** `CompanyName`/`ContactPerson`/`Email`/`Phone`/`Address`/`CreditLimit`/`PaymentTermsDays` are updated but `Code` is unchanged

#### Scenario: A duplicate customer code is rejected
- **WHEN** `POST /api/v1/sales/customers` is called with a `Code` that already exists
- **THEN** the request is rejected with `CustomerCodeDuplicateException`

### Requirement: Invoices follow a Draft → Issued → PartiallyPaid/Paid/Void lifecycle, with Overdue computed at query time
`Invoice` SHALL start as `Draft` with editable `Lines` (`Description`, `Quantity`, `UnitOfMeasure` (nullable free text), `UnitPrice`, optional `TaxCodeId`). `Issue(invoiceNumber)` SHALL only succeed from `Draft`, SHALL assign a sequential number in the format `INV-{year}-{seq:D6}`, and SHALL raise `InvoiceIssuedEvent`. `Void(reason, hasVerifiedPayment)` SHALL reject with `InvoiceHasVerifiedPaymentException` if a verified payment already exists against the invoice. `IsOverdue` SHALL be computed at query time from `DueDate`/`Status` and SHALL never be a stored field or background-job-driven transition.

#### Scenario: Editing lines is rejected once issued
- **WHEN** `PUT /api/v1/sales/invoices/{id}` is called on an invoice whose `Status` is not `Draft`
- **THEN** the request is rejected with `InvoiceNotDraftException`

#### Scenario: Issuing assigns a sequential number and posts to the GL
- **WHEN** `POST /api/v1/sales/invoices/{id}/issue` is called (permission `Sales.Invoice.Approve`) on a Draft invoice
- **THEN** the invoice is assigned the next `INV-{year}-{seq:D6}` number, its `Status` becomes `Issued`, and a `GLEntry` with `Source = SalesAutoPost` is posted debiting Accounts Receivable and crediting Sales Revenue (and SST Payable, if any line is taxed)

#### Scenario: Voiding an invoice with a verified payment is rejected
- **WHEN** `POST /api/v1/sales/invoices/{id}/void` is called (permission `Sales.Invoice.Approve`) on an invoice that has at least one `Verified` payment
- **THEN** the request is rejected with `InvoiceHasVerifiedPaymentException`

#### Scenario: Overdue is derived, not stored
- **WHEN** an `Issued` or `PartiallyPaid` invoice's `DueDate` is before the current date
- **THEN** `GET` responses report `IsOverdue = true` without any stored status transition ever having run

### Requirement: A recorded payment does not affect the invoice until verified
`Payment` SHALL start as `Pending` on `Create`. `Verify(verifiedBy, verifiedAtUtc)` SHALL be the only operation that applies the payment amount to the invoice (via `Invoice.ApplyPayment`, before the payment itself is marked `Verified`) and SHALL raise `PaymentVerifiedEvent`. `Reject(reason)` SHALL require a non-empty reason.

#### Scenario: A Pending payment leaves the invoice balance untouched
- **WHEN** `POST /api/v1/sales/invoices/{id}/payments` records a payment
- **THEN** the payment's `Status` is `Pending` and the invoice's `AmountPaid`/`Status`/`OutstandingBalance` are unchanged

#### Scenario: Verifying a payment applies it and posts to the GL
- **WHEN** `POST /api/v1/sales/payments/{id}/verify` is called (permission `Sales.Payment.Verify`) on a Pending payment
- **THEN** the invoice's `AmountPaid` increases by the payment amount, its `Status` becomes `PartiallyPaid` or `Paid` as appropriate, and a `GLEntry` with `Source = SalesAutoPost` is posted debiting Cash (or the payment's linked `BankAccount`) and crediting Accounts Receivable

#### Scenario: An overpayment is rejected
- **WHEN** a payment `Amount` exceeds the invoice's current `OutstandingBalance`
- **THEN** `Invoice.ApplyPayment` throws `PaymentExceedsOutstandingBalanceException`

#### Scenario: Rejecting a payment requires a reason
- **WHEN** `POST /api/v1/sales/payments/{id}/reject` is called without a `reason`
- **THEN** the request is rejected with `RejectRequiresReasonException`

### Requirement: A credit note is created and applied in a single step, bounded by the invoice's outstanding balance
`CreditNote.Create(creditNoteNumber, invoiceId, reason, amount)` SHALL assign a sequential `CN-{year}-{seq:D6}` number and set `Status = Applied` directly, reducing the target invoice's outstanding balance via `Invoice.ApplyPayment`. It SHALL reject if `amount` exceeds the invoice's `OutstandingBalance`.

#### Scenario: A credit note exceeding the outstanding balance is rejected
- **WHEN** `POST /api/v1/sales/credit-notes` is called with an `Amount` greater than the target invoice's `OutstandingBalance`
- **THEN** the request is rejected with `CreditNoteExceedsInvoiceBalanceException`

### Requirement: Sales GL posting requires SalesSettings to be fully configured
`SalesSettings` SHALL be a single-row entity holding `DefaultReceivableAccountId`, `DefaultRevenueAccountId`, `DefaultTaxPayableAccountId`, and `DefaultCashAccountId`, all nullable until an Admin configures them via `PUT /api/v1/sales/settings` (permission `Sales.Settings.Update`). `IsConfigured` SHALL require all four to be set.

#### Scenario: Issuing an invoice before Sales settings are configured fails at posting time, not at issue time
- **WHEN** an invoice is issued while `SalesSettings.IsConfigured` is false
- **THEN** the GL-posting event handler throws `SalesSettingsNotConfiguredException` (the invoice's own `Issue()` transition itself does not depend on Accounting being configured or licensed)

### Requirement: Sales-to-GL posting is decoupled via domain events, and no-ops when Accounting isn't licensed
`Invoice.Issue()` and `Payment.Verify()` SHALL raise domain events (`InvoiceIssuedEvent`, `PaymentVerifiedEvent`) dispatched via MediatR only after the triggering database transaction commits. The Accounting-side handlers SHALL check `IFeatureFlagService.IsEnabled(Module.Accounting)` and SHALL no-op (post nothing, throw nothing) when Accounting is not licensed.

#### Scenario: Sales works standalone without Accounting licensed
- **WHEN** an invoice is issued or a payment is verified in a deployment whose license does not include the `Accounting` module
- **THEN** the Sales-side transition still succeeds and no `GLEntry` is created

### Requirement: SalesUser can create and read but not approve or verify
The `SalesUser` role SHALL hold Create/Read/Update on Customer, Invoice, Payment, and CreditNote, but SHALL NOT hold `Sales.Invoice.Approve` or `Sales.Payment.Verify`.

#### Scenario: SalesUser is forbidden from verifying a payment
- **WHEN** a user with only the `SalesUser` role calls `POST /api/v1/sales/payments/{id}/verify`
- **THEN** the request is rejected with 403 Forbidden

#### Scenario: SalesUser is forbidden from voiding or issuing an invoice
- **WHEN** a user with only the `SalesUser` role calls `POST /api/v1/sales/invoices/{id}/issue` or `.../void`
- **THEN** the request is rejected with 403 Forbidden

### Requirement: Sales Summary and AR Aging reports are available read-only across roles
`GET /api/v1/sales/reports/sales-summary` (groupBy `customer`, `product`, or `month`) and `GET /api/v1/sales/reports/aging` (bucketed current, 1-30, 31-60, 61-90, 90+ days overdue from `DueDate`) SHALL both require only `Sales.Reports.Read`.

#### Scenario: AR Aging buckets an overdue invoice correctly
- **WHEN** an `Issued` invoice's `DueDate` is 45 days before `asOfDate`
- **THEN** its `OutstandingBalance` appears in that customer's `Days31To60` bucket
