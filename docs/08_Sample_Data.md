# 08 — Sample Data Strategy

Size target: **MEDIUM** — realistic enough to exercise reports, pagination, and edge cases without being unwieldy to review manually.

## 8.1 Volume by Module

| Module | Entity | Volume |
|---|---|---|
| Accounting | Chart of Accounts | 30–40 accounts (standard structure: Assets 1000s, Liabilities 2000s, Equity 3000s, Revenue 4000s, Expense 5000s) |
| Accounting | GL Entries | 100+ transactions, mixed Draft/Posted/Voided |
| Accounting | Bank Accounts | 2–3 (Maybank, CIMB, Public Bank style) |
| Accounting | Tax Codes | SST-6, SST-0, plus income tax reference codes |
| Sales | Customers | ✅ built: 41, Malaysia-based |
| Sales | Products (shared w/ Inventory) | not yet — Products table doesn't exist until Phase 3 |
| Sales | Invoices | ✅ built: 24, various statuses (2 Draft, 22 Issued spanning PartiallyPaid/Paid/Issued incl. overdue) |
| Sales | Payments | ✅ built: 18 (15 Verified, 1 Rejected, 2 Pending) |
| Sales | Credit Notes | ✅ built: 2 |
| Inventory | Products | 30–40 (union with Sales product set) |
| Inventory | Stock Levels | realistic quantities across 2–3 warehouses |
| Inventory | Stock Movements | 30–50 |
| Inventory | Valuation examples | at least 3 products showing FIFO vs weighted-average divergence |
| Identity | Users | 7 (one per seeded role, see below) |

## 8.2 Users & Permissions Seed

| User | Role | Purpose |
|---|---|---|
| superadmin@litxus.demo | Super Admin | install owner — full access including License + FeatureFlags |
| admin@litxus.demo | Admin | full business access, used for approvals |
| accountant@litxus.demo | Accountant | GL entry + reports testing |
| salesuser@litxus.demo | SalesUser | invoice/customer/payment testing — Create/Read/Update on Customer/Invoice/Payment/CreditNote, but no Approve or Verify (verified live: 403 issuing/voiding an invoice or verifying a payment, 201 creating a customer) |
| inventorymanager@litxus.demo | InventoryManager | stock movement testing (Inventory module not built until Phase 3, same caveat as above) |
| manager@litxus.demo | Manager | read-only reports testing |
| viewer@litxus.demo | Viewer | negative-permission testing, verify 403s |

All seeded with password `Demo@12345` (Development/Demo environments only — never seeded in Production). `superadmin@litxus.demo` and `admin@litxus.demo` are implemented and verified (`UserSeeder`, [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.4); the remaining four are documented here as the target state but not yet built.

## 8.3 Realistic Malaysia Context

- **Company names:** e.g. "Tropikal Hardware Sdn Bhd", "Selangor Pipe Supplies Sdn Bhd", "KL Trading Enterprise", "Penang Distributors Sdn Bhd" — mix of Sdn Bhd (private limited) and Enterprise (sole prop) forms matching Companies Act 2016 entity types.
- **Currency:** All amounts MYR, displayed as `RM 1,250.00` (`en-MY` Intl.NumberFormat).
- **Phone numbers:** `+60 3-XXXX XXXX` (landline, KL), `+60 12-XXX XXXX` (mobile) formats.
- **Product categories:** plumbing/hardware-adjacent to reflect a plausible SME distributor (PVC pipes, fittings, tools, safety equipment) — chosen as a neutral, realistic vertical distinct from the unrelated PSMPE project.
- **Addresses:** real Malaysian states/cities (Selangor, Johor Bahru, Penang, Kuala Lumpur) without using real street addresses.

## 8.4 Test Scenario / Edge-Case Data

Seed data deliberately includes:
- ✅ built: several invoices past their due date with no full payment (tests Overdue status + AR aging report buckets)
- ✅ built: several invoices with a partial payment (tests PartiallyPaid status)
- One voided GL entry with a reason (tests audit trail display)
- One product below its reorder level (tests reorder alert) — Phase 3, not built yet
- One customer at/over their credit limit — seeded data doesn't currently exercise this; credit-limit enforcement at invoice-creation time isn't built either (flagged gap, see `docs/phase-2-sales/Features.md`)
- One bank statement line with no matching GL entry (tests unreconciled-item report)

## 8.5 How Seeding Works (as actually built)

- `ISeeder` interface (`Order` + `SeedAsync` + `AlwaysRun`, default `false`), implemented today by `RbacSeeder` (Order 1 — Permissions catalog from code, 7 roles, role→permission grants), `CompanySeeder` (Order 2 — one local/demo company profile), `LicenseSeeder` (Order 3 — one local/demo license, `EnabledModules` now `Accounting,Sales`), `UserSeeder` (Order 4 — one demo account per seeded role, all 7: `superadmin@litxus.demo`, `admin@litxus.demo`, `accountant@litxus.demo`, `salesuser@litxus.demo`, `inventorymanager@litxus.demo`, `manager@litxus.demo`, `viewer@litxus.demo`, via `UserManager<AppUser>` so passwords are hashed correctly, not inserted directly), `AccountingDemoDataSeeder` (Order 5 — 28 Chart of Accounts entries, 2 SST tax codes, 102 GL entry rows across Jan–Jun telling one coherent 6-month SME narrative — 93 Posted, 4 Draft (2 intentionally unbalanced), 5 Voided with real reasons, plus a zero-value and a future-dated entry; checked by `Account.Code` individually rather than "any accounts exist" so it layers on top of accounts a user already created by hand — plus, once Bank Reconciliation shipped, 2 bank accounts (Maybank/CIMB, linked to the already-seeded cash accounts) with statement lines derived from real Posted GL activity, mostly pre-matched with a deliberately unmatched statement line and a couple of unmatched GL lines), `SalesDemoDataSeeder` (Order 6 — Sales Settings configured against the already-seeded Accounting accounts, 41 customers, 24 invoices issued/paid via real domain method calls so the GL auto-posting pipeline actually runs, 18 payments, 2 credit notes; full detail in [phase-2-sales/Sample_Data.md](phase-2-sales/Sample_Data.md)). Run in `Order` by `SeedDatabaseHostedService` on startup.
- Gated by `appsettings.{Environment}.json` → `"Seeding": { "Enabled": true }` — **on** for Local and Demo environments, **off** by default in Production (a production install should never silently get demo data; if a customer wants sample data for training purposes, it's a deliberate, documented admin action) — **except** `RbacSeeder`, which sets `AlwaysRun => true` and runs in every environment regardless of the flag: the permission/role catalog is reference data the app cannot function without (not demo data), and without it a fresh production install's Roles table would stay empty and lock out the first self-registered user. See `docs/10_Deployment.md` §10.3a for the resulting production bootstrap procedure.
- Idempotent, but **not** all one-shot: `CompanySeeder`/`LicenseSeeder`/`UserSeeder` still check their own table is empty before inserting (single-row/fixed-set seed data that never grows). `RbacSeeder` is deliberately **additive on every run** instead — it fetches-or-creates each `Permission` by `Code` and each `Role` by `Name`, then re-grants every role's permission list every startup (`Role.GrantPermission` no-ops if already granted). This was changed during Phase 2: the original "if any permissions exist, skip everything" guard meant Phase 2's new `Sales.*` permissions and role grants never reached an already-seeded database — a new phase's permissions must always be able to reach an install that was seeded before that phase existed, so Phase 3's Inventory permissions will rely on this same additive pattern.
- **Verified**: fresh SQL Server container → migration → API startup → both `superadmin@litxus.demo` and `admin@litxus.demo` log in with the correct roles/permissions/enabledModules, and `admin@litxus.demo` successfully creates a GL account through the full RBAC-gated endpoint. Re-verified for Phase 2: restarting an already-seeded database picks up the new Sales permissions/grants without a manual data fix.
- Raw SQL scripts (`/docs/sample-data/*.sql`) are also generated from the seeders (via a `dotnet run --seed-export` utility) at the end of each phase, for cases where a customer wants to load sample data into a fresh DB without running the full app seeding pipeline (e.g. for a sales demo environment provisioned independently).

## 8.6 Performance Testing Data

For Phase 5 load testing, a separate `PerformanceSeeder` (never run outside a load-test environment) multiplies the above volumes ×10 (1,000+ GL entries, 200–300 invoices, 300–500 stock movements) to validate pagination, report query performance, and index effectiveness under realistic multi-year data volumes.
