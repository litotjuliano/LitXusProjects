## Context

All 4 items were flagged as deliberate, documented gaps in the original Phase 2 build (`docs/phase-2-sales/Features.md`/`Business_Rules.md`), not oversights. This change closes them per explicit user request.

## Goals / Non-Goals

**Goals:** close all 4 flagged gaps without changing any already-shipped Phase 2 behavior.

**Non-Goals:** invoice Excel export (not in the original 15-endpoint spec, unlike the 4 Accounting reports which do have it — not requested, not added); hard-blocking credit limit (explicitly rejected in favor of a soft warning, see Decisions below).

## Decisions

- **Credit Note GL treatment: Dr Sales Revenue / Cr Accounts Receivable, full `Amount`, no SST reversal.** `CreditNote` has no per-line tax breakdown (unlike `Invoice`/`InvoiceLine`), so there's no SST amount to split back out precisely. Reversing the full amount against Revenue only is a deliberate simplification — a future change could add line-level detail to `CreditNote` if precise tax reversal becomes a requirement.
- **Credit-limit check is soft (warning only), never a hard block.** Confirmed via `AskUserQuestion`: the user chose "soft warning" over "hard block," reasoning that a SalesUser may have good reason to extend a trusted customer past their nominal limit. Implemented via a new `meta.creditLimitWarning` field on `POST /sales/invoices`'s response — the first endpoint in this codebase to use the response envelope's `meta` field for anything other than `null`, since every other existing endpoint had nothing extra to report.
- **`CreditLimit <= 0` means "no limit configured,"** not "zero credit allowed" — matches the field's implicit default for customers where an Admin hasn't set a real limit yet, avoiding false-positive warnings on the ~majority of seeded customers that don't have a limit set meaningfully low.
- **Invoice PDF reuses `QuestPdfReportExporter`'s page-layout/cell helper methods** (made `internal static` rather than `private static`) instead of duplicating the QuestPDF boilerplate in a new class — same assembly, same "one document layout convention" as the 4 existing Accounting report PDFs.
- **Concurrent-numbering test hits the real HTTP API with 20 parallel `Issue` calls** rather than calling `NumberSequenceGenerator` directly — proves the guarantee holds through the full stack (controller → handler → domain method → SQL `SEQUENCE`), matching this session's established preference for testing through the real interface, not an internal shortcut.

## Risks / Trade-offs

- [Credit Note GL reversal doesn't reverse SST Payable] → Acceptable for now; flagged here for whoever adds line-level credit note detail later.
- [`meta.creditLimitWarning` is the first non-null use of the envelope's `meta` field] → No other endpoint follows this convention yet; a future PR standardizing "warnings" across more endpoints (e.g. overdue-customer warnings elsewhere) should reuse this exact shape (`{ creditLimitWarning: string | null }` today, likely generalized to a `warnings: string[]` array if a second warning type is added) rather than inventing a new one per endpoint.
