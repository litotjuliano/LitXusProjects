# Phase 2 — Test Scenarios

Structured per [09_Testing_Strategy.md](../09_Testing_Strategy.md) §9.4 categories. Given/When/Then format; each maps to a unit or integration test (see `InvoiceTests.cs`, `PaymentTests.cs`, `CreditNoteTests.cs`, `InvoiceToGLPostingTests.cs`, `CreditNoteToGLPostingTests.cs`, `CreditLimitWarningTests.cs`, `ConcurrentInvoiceNumberingTests.cs`) or was verified live.

## Customers

### Happy path
- Given a unique code/company name, When `POST /sales/customers`, Then 201 and the customer appears in the list.
- Given an existing customer, When deactivated, Then it's excluded from the invoice customer picker but still visible with "Show inactive" checked.

### Error cases
- Given a duplicate code, When `POST /sales/customers`, Then rejected with `CustomerCodeDuplicateException`.
- Given an attempt to change `code` via `PUT`, Then the field is ignored, code remains unchanged.

## Invoices

### Happy path
- Given a Draft invoice with 1 taxed and 1 untaxed line, When `POST /sales/invoices/{id}/issue`, Then it's assigned `INV-YYYY-NNNNNN`, flips to `Issued`, and a `Posted` GL entry appears with `Source = SalesAutoPost` (Dr AR / Cr Revenue / Cr SST Payable). — covered live and by `InvoiceToGLPostingTests`.
- Given an `Issued` invoice with no payments, When voided with a reason, Then it flips to `Void` and the reason is recorded.
- Given a `DueDate` in the past on an `Issued` invoice, When queried, Then `IsOverdue = true` is returned without any stored transition.

### Error cases
- Given an `Issued` invoice, When `PUT /sales/invoices/{id}` is attempted, Then rejected with `InvoiceNotDraftException`.
- Given an `Issued` invoice with a `Verified` payment against it, When voided, Then rejected with `InvoiceHasVerifiedPaymentException`.
- Given a void attempt with an empty reason, Then rejected with `InvoiceVoidRequiresReasonException`.

### Edge cases
- Given an invoice issued while Sales Settings are unconfigured, When the event handler runs, Then `SalesSettingsNotConfiguredException` is thrown but the invoice's own `Issue()` transition still succeeded (verified: the invoice is `Issued` even though no GL entry exists).
- Given a deployment licensed for Sales only (no Accounting), When an invoice is issued, Then it succeeds and no GL entry is created.
- Given a customer with `CreditLimit = 1000` and RM 800 already outstanding, When a new RM 300 invoice is created, Then it succeeds (201) with `meta.creditLimitWarning` describing the RM 1,100 projected outstanding — covered by `CreditLimitWarningTests` and verified live via direct API call.
- Given a customer with `CreditLimit = 0` (no limit configured), When any invoice amount is created, Then `meta.creditLimitWarning` is always `null`.
- Given 20 Draft invoices for the same customer, When all 20 are issued via concurrent `POST .../issue` requests, Then all 20 returned invoice numbers are distinct — covered by `ConcurrentInvoiceNumberingTests`.
- Given an existing invoice, When `GET /sales/invoices/{id}/pdf` is called, Then the response is a valid PDF (`%PDF-` header) — verified live via a real browser download.

## Payments

### Happy path
- Given an `Issued` invoice, When a payment is recorded for the full outstanding amount, Then it's `Pending` and the invoice is untouched.
- Given a `Pending` payment, When `POST /sales/payments/{id}/verify`, Then the invoice's `AmountPaid`/`Status`/`OutstandingBalance` update, a second GL entry posts (Dr Cash / Cr AR), and the payment is `Verified`. — covered live end-to-end.
- Given a `Pending` payment, When `POST /sales/payments/{id}/reject { reason }`, Then it's `Rejected` and the invoice is untouched.

### Error cases
- Given a payment amount greater than the invoice's outstanding balance, When verified, Then rejected with `PaymentExceedsOutstandingBalanceException` and the payment stays `Pending`.
- Given a reject attempt with an empty reason, Then rejected with `RejectRequiresReasonException`.
- Given an already-`Verified` payment, When verify or reject is attempted again, Then rejected with `PaymentNotPendingException`.

## Credit Notes

### Happy path
- Given an `Issued` invoice with RM 500 outstanding, When a RM 200 credit note is created, Then it's assigned `CN-YYYY-NNNNNN`, `Status = Applied`, the invoice's outstanding balance drops to RM 300, and a `Posted` GL entry appears (Dr Sales Revenue RM 200 / Cr Accounts Receivable RM 200) — covered by `CreditNoteToGLPostingTests` and verified live.

### Error cases
- Given an invoice with RM 500 outstanding, When a RM 600 credit note is attempted, Then rejected with `CreditNoteExceedsInvoiceBalanceException`.

## RBAC / Security

- Given a `SalesUser`-only token, When `POST /sales/payments/{id}/verify` is called, Then 403. — verified live.
- Given a `SalesUser`-only token, When `POST /sales/invoices/{id}/issue` or `.../void` is called, Then 403. — verified live.
- Given a `SalesUser`-only token, When `POST /sales/customers` is called, Then 201 (Create is granted). — verified live.
- Given no `Sales` module in the license, When any `/sales/*` endpoint is called, Then 403 regardless of role/permission.

## Reports

- Given seeded invoices spanning 6 months, When `GET /sales/reports/sales-summary?groupBy=month`, Then each month's `InvoiceCount`/`SubTotal`/`SSTAmount`/`TotalAmount` sum correctly and `GrandTotal` matches their sum.
- Given an invoice 45 days past its due date, When `GET /sales/reports/aging`, Then its outstanding balance appears in that customer's `Days31To60` bucket.
