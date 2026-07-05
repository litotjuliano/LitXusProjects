## Why

The initial Phase 2 Sales build (`2026-07-05-sales-module`) shipped with 4 documented, deliberate gaps: no GL posting for Credit Notes, no invoice PDF export, no credit-limit enforcement at invoice creation, and no concurrent invoice-numbering load test. The user asked to close all 4.

## What Changes

- `CreditNote.Create()` now raises `CreditNoteAppliedEvent`, handled by a new `PostCreditNoteToGLHandler` (Dr Sales Revenue / Cr Accounts Receivable, reversing the invoice's revenue posting for the credit amount) — same domain-event pattern as invoice issuance/payment verification.
- New `GET /api/v1/sales/invoices/{id}/pdf` endpoint (`IInvoicePdfExporter`/`QuestPdfInvoiceExporter`, reusing `QuestPdfReportExporter`'s page-layout helpers) plus a "Download PDF" button on the invoice detail view.
- `POST /api/v1/sales/invoices` now returns a soft, non-blocking `meta.creditLimitWarning` when the customer's projected outstanding balance (existing Issued/PartiallyPaid invoices + the new one) would exceed their `CreditLimit`. Invoice creation is never rejected for this — confirmed with the user this should be a warning, not a hard block. `CreditLimit <= 0` means no limit configured, matching the field's default.
- New integration test proving invoice numbering stays unique under 20 concurrent `Issue` calls (backed by the existing SQL Server `SEQUENCE`, not new production code).

## Capabilities

### Modified Capabilities
- `sales`: adds Credit Note GL posting, invoice PDF export, and the credit-limit warning requirement.

### New Capabilities
None.

## Impact

- Backend: `CreditNoteAppliedEvent`, `PostCreditNoteToGLHandler`, `IInvoicePdfExporter`/`QuestPdfInvoiceExporter`, `CreateInvoiceResultDto`, `CreateInvoiceCommandHandler` (credit-limit check), `InvoicesController` (`/pdf` endpoint, updated `Create` response shape).
- Frontend: `helpers/api/sales.ts` (`getInvoicePdf`), `Invoices.tsx` (Download PDF button, credit-limit warning alert on create).
- Tests: `CreditNoteToGLPostingTests`, `CreditLimitWarningTests` (3 cases), `ConcurrentInvoiceNumberingTests`.
- No database schema changes.
