# Phase 2 — LitXus Retail Pro, Part 1 — Features

Goal: a fully standalone Sales module (Customers, Invoices, Payments, Credit Notes, 2 reports) that works whether or not Accounting is licensed alongside it, auto-posting to the GL when it is.

---

## Feature 1: Customer Master

**Priority:** Must-have
**User story:** As a SalesUser, I want to maintain a customer list, so that invoices have somewhere valid to bill.

**Acceptance criteria:**
- [x] Create a customer: code, company name, contact person, email, phone, address, credit limit, payment terms (days)
- [x] Customer codes are unique and immutable once set (editing a customer never changes its code)
- [x] Customers can be deactivated/reactivated (`IsActive`) — never deleted, matching Accounting's `Account` convention
- [x] Inactive customers are excluded from the invoice customer picker by default

**Out of scope:** Credit-limit enforcement at invoice-creation time (the field is stored but not yet checked against outstanding balance — flagged as a gap, not built this pass).

---

## Feature 2: Invoice Lifecycle (Draft → Issued → PartiallyPaid/Paid/Void)

**Priority:** Must-have
**User story:** As a SalesUser, I want to build an invoice, issue it, and track what's still owed, so that revenue is billed and collected correctly.

**Acceptance criteria:**
- [x] Create a Draft invoice with 1+ free-text lines (Description, Quantity, optional `UnitOfMeasure`, Unit Price, optional Tax Code)
- [x] Draft invoices are fully editable (lines can be replaced); Issued invoices are not
- [x] Issuing assigns a sequential number (`INV-2026-000123`) and auto-posts a GL entry (if Accounting is licensed and Sales Settings are configured)
- [x] `Overdue` is computed at query time from `DueDate` vs. today — never a stored transition, since no scheduled-job infrastructure exists in this codebase
- [x] Voiding requires a reason and is rejected outright if a Verified payment already exists against the invoice
- [x] `AmountPaid`/`OutstandingBalance`/`Status` (PartiallyPaid/Paid) update automatically as payments are verified

**Out of scope:** A real `ProductId` line reference (Phase 3 Inventory territory — every Phase 2 line is free-text); a multi-level Carton/Box/Pack unit-of-measure conversion engine (also Phase 3 scope — `UnitOfMeasure` here is a single free-text convenience field, e.g. "pcs" or "kg").

---

## Feature 3: Payment Recording & Admin Verification

**Priority:** Must-have
**User story:** As an Admin, I want payments verified before they affect the books, so that unconfirmed bank transfers can't be booked as revenue collected.

**Acceptance criteria:**
- [x] Record a payment against an Issued/PartiallyPaid invoice (date, amount, method, optional reference, optional linked bank account) — starts `Pending` and does **not** touch the invoice balance yet
- [x] Verify a Pending payment (`Sales.Payment.Verify`, Admin-only) — applies the amount to the invoice, flips its status, and auto-posts a GL entry (Dr Cash/Bank, Cr Accounts Receivable)
- [x] Reject a Pending payment with a required reason — no effect on the invoice
- [x] An overpayment (amount exceeding the invoice's outstanding balance) is rejected outright, whether recorded as a single payment or attempted via Verify

**Out of scope:** Partial refunds on a rejected payment (rejection is terminal, no retry path other than recording a new payment).

---

## Feature 4: Credit Notes

**Priority:** Must-have
**User story:** As a SalesUser, I want to issue a credit note against an invoice, so that returns/adjustments reduce what the customer still owes.

**Acceptance criteria:**
- [x] Create a credit note against one invoice (reason, amount) — assigned a sequential number (`CN-2026-000123`) and applied immediately (single-step, no separate Draft/Issue states)
- [x] Amount exceeding the invoice's current outstanding balance is rejected
- [x] Credit notes reduce the invoice's outstanding balance the same way a verified payment does

**Out of scope:** GL posting for credit notes (flagged as a real gap for a future change, not built this pass) — a credit note currently affects the invoice balance only, not the ledger.

---

## Feature 5: Sales GL Auto-Posting (cross-module, standalone-safe)

**Priority:** Must-have
**User story:** As an Accountant, I want invoice issuance and payment verification to post to the GL automatically, so I never have to manually journal routine sales activity — and as a Sales-only customer, I want the module to work even without Accounting.

**Acceptance criteria:**
- [x] An Admin configures 4 default GL accounts once (Sales Settings: Receivable, Revenue, Tax Payable, Cash) before any posting can succeed
- [x] Issuing an invoice posts Dr Accounts Receivable / Cr Sales Revenue (+ Cr SST Payable if any line is taxed), already `Posted` (never left Draft)
- [x] Verifying a payment posts Dr Cash (or the payment's linked bank account) / Cr Accounts Receivable
- [x] Both postings go through a real domain-event pipeline (`InvoiceIssuedEvent`/`PaymentVerifiedEvent`, dispatched via MediatR after the triggering transaction commits) — Sales has no compile-time dependency on Accounting
- [x] In a deployment licensed for Sales only (no Accounting), invoices still issue and payments still verify normally; simply no GL entry is created

**Out of scope:** GL posting for credit notes (see Feature 4).

---

## Feature 6: Sales Reports

**Priority:** Should-have
**User story:** As a Manager, I want summary and aging views of sales, so I can see revenue trends and who owes what.

**Acceptance criteria:**
- [x] Sales Summary — grouped by customer, product (free-text `Description`, since no Product entity exists yet), or month, for a date range
- [x] AR Aging — outstanding balances bucketed Current / 1-30 / 31-60 / 61-90 / 90+ days overdue, as of a chosen date
- [x] Both reports render as plain tables, matching the existing Accounting reports' convention (no Recharts — not currently a frontend dependency, and every existing report is a plain table)

**Out of scope:** CSV/PDF/Excel export for the 2 Sales reports (the 4 Accounting reports have this; Sales reports don't yet — a real gap, not built this pass).

---

## Feature 7: RBAC for Sales

**Priority:** Must-have
**User story:** As an Admin, I want Sales staff to create and read but not approve their own transactions, so approvals stay a control point.

**Acceptance criteria:**
- [x] `SalesUser` role: Create/Read/Update on Customer, Invoice, Payment, Credit Note — but **not** `Sales.Invoice.Approve` (gates both Issue and Void) or `Sales.Payment.Verify`
- [x] Super Admin/Admin hold every Sales permission; Accountant/Manager/Viewer hold `Sales.*.Read` only
- [x] Verified live: a `SalesUser` gets 403 on Issue, Void, and Verify Payment, but 201 creating a customer
