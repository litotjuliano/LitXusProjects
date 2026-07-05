# Phase 2 — Business Rules

## Rule: Customer codes are unique and immutable after creation
**Statement:** `Customers.Code` is unique; once set, `PUT /sales/customers/{id}` can change `CompanyName`/`ContactPerson`/`Email`/`Phone`/`Address`/`CreditLimit`/`PaymentTermsDays` but never `Code`.
**Enforced at:** DB unique index + `CreateCustomerCommandHandler` (checks for an existing code before insert).
**Violation behavior:** `CustomerCodeDuplicateException` on create.

## Rule: Only Draft invoices are editable
**Statement:** Once an invoice is `Issued` (or later), its lines, dates, and notes are immutable — corrections happen via a Credit Note, never by editing an issued invoice.
**Enforced at:** `Invoice.UpdateLines()` domain method (`EnsureIsDraft()`).
**Violation behavior:** `InvoiceNotDraftException`.

## Rule: Issuing assigns a sequential, never-reused invoice number
**Statement:** `InvoiceNumber` (`INV-YYYY-NNNNNN`) is assigned only at issue time (never at Draft creation, since a Draft may never be issued), via a SQL Server `SEQUENCE` object — the same pattern as `JE-YYYY-NNNNNN` for GL entries.
**Enforced at:** `Invoice.Issue()` calls `INumberSequenceGenerator.NextInvoiceNumberAsync()`.
**Violation behavior:** N/A — a correctness guarantee, not a rejection path.

## Rule: An invoice with a verified payment cannot be voided
**Statement:** `POST /sales/invoices/{id}/void` is rejected outright if any payment against that invoice has `Status = Verified` — money has already been confirmed collected, so voiding would silently misstate the books.
**Enforced at:** `VoidInvoiceCommandHandler` (checks for a Verified payment before calling `Invoice.Void`) + `Invoice.Void(reason, hasVerifiedPayment)`.
**Violation behavior:** `InvoiceHasVerifiedPaymentException`.

## Rule: Voiding an invoice requires a reason
**Statement:** `POST /sales/invoices/{id}/void` requires a non-empty `reason`.
**Enforced at:** `Invoice.Void()`.
**Violation behavior:** `InvoiceVoidRequiresReasonException`.

## Rule: A recorded payment does not affect the invoice until verified
**Statement:** `Payment.Create()` starts `Pending` and has zero effect on `Invoice.AmountPaid`/`Status`/`OutstandingBalance`. Only `Payment.Verify()` calls `Invoice.ApplyPayment()` — and does so *before* flipping the payment itself to `Verified`, so a failed apply (e.g. overpayment) never leaves a falsely-verified payment.
**Enforced at:** `VerifyPaymentCommandHandler`.
**Violation behavior:** N/A for recording; see overpayment rule below for the rejection path.

## Rule: A payment cannot exceed the invoice's outstanding balance
**Statement:** `Invoice.ApplyPayment(amount)` rejects if `amount > OutstandingBalance` (`TotalAmount - AmountPaid`).
**Enforced at:** `Invoice.ApplyPayment()`, called from `VerifyPaymentCommandHandler`.
**Violation behavior:** `PaymentExceedsOutstandingBalanceException`, carrying the actual outstanding balance.

## Rule: Rejecting a payment requires a reason
**Statement:** `POST /sales/payments/{id}/reject` requires a non-empty `reason`; a Rejected payment has no further effect on the invoice.
**Enforced at:** `Payment.Reject()`.
**Violation behavior:** `RejectRequiresReasonException`.

## Rule: A payment can only be verified or rejected once
**Statement:** `Verify()`/`Reject()` both require `Status == Pending`; a Verified or Rejected payment is terminal.
**Enforced at:** `Payment.EnsureIsPending()`.
**Violation behavior:** `PaymentNotPendingException`.

## Rule: A credit note cannot exceed the invoice's outstanding balance
**Statement:** `POST /sales/credit-notes` is rejected if `Amount` exceeds the target invoice's current `OutstandingBalance`. A credit note is created and applied in one step (`Status = Applied` directly) — there is no separate Draft/Issue state, since the API only exposes create + read.
**Enforced at:** `CreateCreditNoteCommandHandler` (checked before calling `CreditNote.Create`).
**Violation behavior:** `CreditNoteExceedsInvoiceBalanceException`.

## Rule: GL posting requires Sales Settings to be fully configured
**Statement:** `SalesSettings` (Receivable/Revenue/Tax Payable/Cash account IDs) must all be set (`IsConfigured`) before an invoice issuance or payment verification can post to the GL. Issuing an invoice or verifying a payment itself never depends on this — only the GL-posting event handler does, so Sales still functions without Accounting configured at all.
**Enforced at:** `PostInvoiceToGLHandler`/`PostPaymentToGLHandler`.
**Violation behavior:** `SalesSettingsNotConfiguredException`, thrown inside the event handler (does not roll back the invoice/payment transition itself, since the domain event fires only after that transaction has already committed).

## Rule: Sales-to-GL posting no-ops when Accounting isn't licensed
**Statement:** `PostInvoiceToGLHandler`/`PostPaymentToGLHandler` check `IFeatureFlagService.IsEnabled(Module.Accounting)` first and do nothing (no GL entry, no exception) if Accounting is not licensed — this is what lets Sales work standalone.
**Enforced at:** Both event handlers, as their first check.
**Violation behavior:** N/A — this is a no-op path, not a rejection.

## Rule: Overdue is computed, never stored
**Statement:** `Invoice.IsOverdue(today)` returns `Status is Issued or PartiallyPaid && DueDate < today` at query time. There is no scheduled job that flips a stored status, since no such infrastructure exists in this codebase.
**Enforced at:** `Invoice.IsOverdue()`, called at DTO-mapping time with the current date.
**Violation behavior:** N/A (derived value, not a rejection).

## Rule: SalesUser cannot approve invoices or verify payments
**Statement:** The `SalesUser` role holds Create/Read/Update on Customer/Invoice/Payment/CreditNote but not `Sales.Invoice.Approve` (which gates both Issue and Void) or `Sales.Payment.Verify`.
**Enforced at:** `[RequirePermission]` action filters on `InvoicesController`'s Issue/Void actions and `PaymentsController`'s Verify/Reject actions.
**Violation behavior:** 403, generic "You do not have permission to perform this action" — matches Phase 1's deliberate non-disclosure of which specific permission is missing.

## Rule: Applying a credit note posts a reversing GL entry
**Statement:** `CreditNote.Create()` raises `CreditNoteAppliedEvent`, posting Dr Sales Revenue / Cr Accounts Receivable for the full credit `Amount`. Unlike invoice/payment posting, SST Payable is never adjusted — `CreditNote` has no per-line tax breakdown to reverse precisely.
**Enforced at:** `PostCreditNoteToGLHandler` (same no-op-if-Accounting-unlicensed and Sales-Settings-required rules as invoice/payment posting apply here too).
**Violation behavior:** `SalesSettingsNotConfiguredException` if Sales Settings aren't configured; otherwise none — this is a posting action, not a rejection path.

## Rule: A credit-limit-exceeding invoice warns but is never blocked
**Statement:** `POST /sales/invoices` always succeeds. If the customer's `CreditLimit > 0` and their outstanding balance across `Issued`/`PartiallyPaid` invoices plus the new invoice's `TotalAmount` exceeds it, the response's `meta.creditLimitWarning` carries a message; a `CreditLimit` of 0 or less means no limit is configured and never produces a warning.
**Enforced at:** `CreateInvoiceCommandHandler.BuildCreditLimitWarningAsync`.
**Violation behavior:** N/A — deliberately non-blocking (confirmed with the user: a SalesUser may have good reason to extend a trusted customer past their nominal limit).
