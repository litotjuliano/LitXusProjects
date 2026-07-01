# Phase 1 — Sample Data

Volumes per [08_Sample_Data.md](../08_Sample_Data.md) §8.1, detailed here for the Accounting-only seeder.

## Users (6, per §8.2)

| Email | Role | Password (Dev/Demo only) |
|---|---|---|
| admin@litxus.demo | Admin | Demo@12345 |
| accountant@litxus.demo | Accountant | Demo@12345 |
| sales@litxus.demo | SalesUser | Demo@12345 |
| inventory@litxus.demo | InventoryManager | Demo@12345 |
| manager@litxus.demo | Manager | Demo@12345 |
| viewer@litxus.demo | Viewer | Demo@12345 |

## Chart of Accounts (~35 accounts)

```
1000 Assets
  1010 Cash - Maybank Current
  1020 Cash - CIMB Savings
  1030 Accounts Receivable
  1040 Inventory (placeholder, populated in Phase 3)
  1050 Prepaid Expenses
1100 Fixed Assets
  1110 Office Equipment
  1120 Accumulated Depreciation - Equipment
2000 Liabilities
  2010 Accounts Payable
  2100 Accrued Liabilities
  2200 SST Payable
2300 Loans Payable
3000 Equity
  3010 Share Capital
  3020 Retained Earnings
4000 Revenue
  4010 Sales Revenue (placeholder, populated in Phase 2)
  4020 Service Revenue
5000 Expenses
  5100 Rent Expense
  5110 Utilities Expense
  5120 Salaries Expense
  5130 Office Supplies Expense
  5140 Bank Charges
  5150 Depreciation Expense
  ... (rounding out to ~35 total across the 5 top-level types with 2-3 sub-accounts each)
```

## GL Entries (100+)

Generated programmatically by `Phase1AccountingSeeder` across a 6-month date range, covering:
- ~85 routine Posted entries (rent, utilities, salaries, sales/service revenue recognition, bank charges) with realistic recurring patterns
- 10 Draft entries (mix of balanced and intentionally-unbalanced-for-UI-testing — the unbalanced ones are clearly marked in a seeder comment and never posted by the seeder itself)
- 5 Voided entries, each with a realistic `Reason` ("Duplicate entry", "Wrong account", "Entered in error - see JE-2026-XXXXXX")

Specific edge-case rows required by [Test_Scenarios.md](Test_Scenarios.md):
- One entry with exactly 2 lines both at RM 0.00 (zero-value balanced entry)
- One Posted entry dated in the future relative to seed run time

## Tax Codes

| Code | Rate | Type |
|---|---|---|
| SST-6 | 6.00% | SST |
| SST-0 | 0.00% | SST |

## Bank Accounts (2)

| Bank | Account Number | Linked GL Account |
|---|---|---|
| Maybank | 5641 XXXX 1234 | 1010 Cash - Maybank Current |
| CIMB | 7012 XXXX 5678 | 1020 Cash - CIMB Savings |

## Bank Statement Lines

~15 lines per bank account imported from a seeded CSV fixture, ~80% pre-matched to existing GL entry cash lines, remaining ~20% deliberately left unmatched (required by Test_Scenarios.md's unreconciled-item coverage).

## Seeder Implementation Note

`Phase1AccountingSeeder : ISeeder` runs after the Identity/RBAC seeder (dependency order: Roles/Permissions → Users → TaxCodes → Accounts → GLEntries → BankAccounts → BankStatementLines, since later seeders reference earlier ones by FK). Idempotency check: `if (await _context.Accounts.AnyAsync()) return;` at the top of the accounting portion, consistent with [08_Sample_Data.md](../08_Sample_Data.md) §8.5.
