# Phase 2 — Sample Data

Describes what's **actually seeded** by `SalesDemoDataSeeder` (`Order => 6`, runs after `AccountingDemoDataSeeder` since it references already-seeded Accounts) — per [08_Sample_Data.md](../08_Sample_Data.md)'s targets for Sales.

## Sales Settings

Configured once, idempotently, pointing at Phase 1's already-seeded accounts:
- Receivable → `1030 Accounts Receivable`
- Revenue → `4010 Sales Revenue`
- Tax Payable → `2200 SST Payable`
- Cash → `1010 Cash - Maybank Current`

## Customers (41)

Realistic Malaysian company names, coded `CUST-001` through `CUST-041`, each with a contact person, credit limit, and payment terms (30/60 days). All active.

## Invoices (24, across a 6-month simulated window)

- 22 issued via real `Invoice.Issue()` domain calls (not just inserted pre-issued) — this also exercises real GL auto-posting for every one of them
- 2 left `Draft`
- Spans every status the lifecycle supports: several `Paid` (fully collected), several `PartiallyPaid`, several still `Issued` (some deliberately past their due date to populate the AR Aging report's overdue buckets), 2 `Draft`
- Line items are realistic hardware/trading goods with a mix of taxed (SST-6) and untaxed lines

## Payments (18)

- 15 `Verified` via real `Payment.Verify()` + `Invoice.ApplyPayment()` calls — each posts a real GL entry
- 1 `Rejected` (with a reason)
- 2 left `Pending` (deliberately, so a fresh install has something to demonstrate Verify/Reject on)

## Credit Notes (2)

Issued against invoices with remaining outstanding balance, each with a realistic reason (e.g. damaged goods returned).

## Resulting GL Activity (verified via direct SQL against the seeded database)

- 37 `GLEntries` rows with `Source = SalesAutoPost` (22 from invoice issuance + 15 from payment verification)
- Spot-checked example (6% SST invoice): Dr Accounts Receivable 1,060.00 / Cr Sales Revenue 1,000.00 / Cr SST Payable 60.00
- Spot-checked example (payment verification): Dr Cash 5,724.00 / Cr Accounts Receivable 5,724.00
