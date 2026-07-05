## 1. Credit Note GL Posting

- [x] 1.1 `CreditNoteAppliedEvent` (Domain), raised by `CreditNote.Create()`
- [x] 1.2 `PostCreditNoteToGLHandler` (Dr Sales Revenue / Cr Accounts Receivable)
- [x] 1.3 `CreditNoteToGLPostingTests` — integration test proving the posting fires end-to-end

## 2. Invoice PDF Export

- [x] 2.1 `IInvoicePdfExporter` interface + `QuestPdfInvoiceExporter` (reuses `QuestPdfReportExporter`'s layout helpers)
- [x] 2.2 `GET /sales/invoices/{id}/pdf` endpoint
- [x] 2.3 Frontend "Download PDF" button on the invoice detail view

## 3. Credit-Limit Warning

- [x] 3.1 Confirmed with user: soft warning, not a hard block
- [x] 3.2 `CreateInvoiceResultDto`, `CreateInvoiceCommandHandler` credit-limit check, `meta.creditLimitWarning` on `POST /sales/invoices`
- [x] 3.3 Frontend: surface the warning via alert after invoice creation
- [x] 3.4 `CreditLimitWarningTests` — 3 cases (over limit, within limit, no limit configured)

## 4. Concurrent Invoice Numbering

- [x] 4.1 `ConcurrentInvoiceNumberingTests` — 20 concurrent `Issue` calls, all numbers unique

## 5. Documentation

- [x] 5.1 OpenSpec `sales` capability delta (this change)
- [x] 5.2 `docs/phase-2-sales/` doc set updated (gaps closed)
- [x] 5.3 Root doc updates (`docs/03_API_Specification.md`, `docs/05_Phase_Breakdown.md`)

## 6. Verification

- [x] 6.1 Backend + frontend build clean, full test suite passes (95/95: 77 unit + 18 integration)
- [x] 6.2 Live: downloaded a real invoice PDF (valid `%PDF-` header), created a credit note and confirmed its GL entry, confirmed the credit-limit warning fires via direct API call
