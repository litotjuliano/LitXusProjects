# Phase 2 — UI Mockups

Design language matches Phase 1 exactly: Tailwind, `DataTable` (search/sort/paginate) + `ModalLayout`/`VerticalForm` for create/edit, per-row action buttons gated by permission. New "Sales" nav section only renders once the license's `EnabledModules` includes `Sales`.

## Screen: Customers

```
┌────────────────────────────────────────────────────────┐
│ Customers                                [+ New Customer]│
│ [ ] Show inactive customers                              │
│ [Search by code, company, or email...]                    │
│                                                            │
│ Code       Company              Contact   Credit  Terms  Status   │
│ CUST-001   Petaling Jaya Retail  Ahmad    5,000   30d   Active  [Edit][Deactivate]
│ CUST-002   Subang Jaya Building  Mei Ling 10,000  60d   Active  [Edit][Deactivate]
│ ...                                                        │
└────────────────────────────────────────────────────────┘
```
"+ New Customer" and Edit both open the same modal (code field is create-only; edit shows the code as read-only text).

## Screen: Invoices

```
┌────────────────────────────────────────────────────────────┐
│ Invoices                                        [+ New Invoice]│
│ [Status: All ▾]                                                │
│ [Search by number or customer...]                                │
│                                                                  │
│ Number          Customer            Date    Due     Status         Total    Outstanding │
│ INV-2026-000018 Cyberjaya Tech      05-11   06-10   Issued(Overdue) 3,339.00 3,339.00 [Void]
│ INV-2026-000023 Taiping Industrial  07-05   08-04   Paid            1,669.50     0.00        │
│ INV-2026-000024 —(Draft)—           07-05   08-04   Draft           1,575.00 1,575.00 [Edit][Issue]
└────────────────────────────────────────────────────────────┘
```
Clicking the invoice number opens a detail modal:

```
┌──────────────────────────────────────────────────┐
│ INV-2026-000023 — Taiping Industrial Trading         │
│ Issued                        Total: RM 1,669.50     │
│                                Outstanding: RM 0.00   │
│                                                        │
│ Description    Qty  UOM   Unit Price  Line Total       │
│ PVC Pipe 6in    18  pcs    87.50       1,575.00         │
│                                                        │
│ [Record Payment form — only shown while              │
│  Outstanding > 0 and Status is Issued/PartiallyPaid] │
│  Payment Date [__] Amount [____] Method [Bank Transfer ▾]│
│  Reference [____]  Bank Account [Use default cash acct ▾]│
│                              [ Record Payment ]        │
└──────────────────────────────────────────────────┘
```
The New/Edit Invoice modal is a line-item editor (matches `GLEntries.tsx`'s multi-line editor shape): one row per line (Description, Qty, UOM, Unit Price, Tax dropdown, computed Line Total), "+ Add Line", and a running Subtotal / SST / Total footer.

## Screen: Payments

```
┌────────────────────────────────────────────────────────┐
│ Payments                                                    │
│ [Status: Pending ▾]                                          │
│ [Search by invoice or reference...]                          │
│                                                                │
│ Invoice          Date     Amount     Method        Reference  Status    │
│ INV-2026-000018  05-16   3,339.00   BankTransfer   REF-2017   Pending [Verify][Reject]
│ INV-2026-000017  05-10   1,457.50   BankTransfer   REF-2016   Pending [Verify][Reject]
└────────────────────────────────────────────────────────┘
```
Verify/Reject buttons only render for `Pending` rows; Reject prompts for a reason. Both are gated by `Sales.Payment.Verify` — a `SalesUser` sees the row but the buttons 403 if clicked (matches Phase 1's "server-enforced regardless of UI" rule).

## Screen: Credit Notes

```
┌────────────────────────────────────────────────────┐
│ Credit Notes                          [+ New Credit Note]│
│                                                          │
│ Number       Invoice            Reason           Amount   Status  │
│ CN-2026-001  INV-2026-000005    Damaged goods     250.00  Applied │
└────────────────────────────────────────────────────┘
```
The create form's Invoice dropdown only lists invoices with `OutstandingBalance > 0` and a non-Draft, non-Void status.

## Screen: Sales Settings (Admin)

```
┌────────────────────────────────────────────┐
│ Sales Settings                                  │
│ These GL accounts are used to automatically     │
│ post journal entries...                          │
│                                                    │
│ Accounts Receivable  [1030 Accounts Receivable ▾]  │
│ Sales Revenue        [4010 Sales Revenue ▾]        │
│ SST Payable          [2200 SST Payable ▾]          │
│ Default Cash / Bank  [1010 Cash - Maybank ▾]       │
│                                    [ Save Settings ] │
└────────────────────────────────────────────┘
```

## Screens: Sales Reports

Both `Sales Summary` (groupBy Customer/Product/Month selector + date range) and `AR Aging` (as-of date picker) render as plain tables with a totals footer row, matching `TrialBalance.tsx`/`GeneralLedger.tsx`'s exact convention — no charts.
