# 08 — Sample Data Strategy

Size target: **MEDIUM** — realistic enough to exercise reports, pagination, and edge cases without being unwieldy to review manually.

## 8.1 Volume by Module

| Module | Entity | Volume |
|---|---|---|
| Accounting | Chart of Accounts | 30–40 accounts (standard structure: Assets 1000s, Liabilities 2000s, Equity 3000s, Revenue 4000s, Expense 5000s) |
| Accounting | GL Entries | 100+ transactions, mixed Draft/Posted/Voided |
| Accounting | Bank Accounts | 2–3 (Maybank, CIMB, Public Bank style) |
| Accounting | Tax Codes | SST-6, SST-0, plus income tax reference codes |
| Sales | Customers | 30–50, Malaysia-based |
| Sales | Products (shared w/ Inventory) | 20–30 |
| Sales | Invoices | 20–30, various statuses (Draft/Issued/PartiallyPaid/Paid/Void/Overdue represented) |
| Sales | Payments | 15–20 |
| Sales | Credit Notes | 2–3 |
| Inventory | Products | 30–40 (union with Sales product set) |
| Inventory | Stock Levels | realistic quantities across 2–3 warehouses |
| Inventory | Stock Movements | 30–50 |
| Inventory | Valuation examples | at least 3 products showing FIFO vs weighted-average divergence |
| Identity | Users | 6 (one per seeded role, see below) |

## 8.2 Users & Permissions Seed

| User | Role | Purpose |
|---|---|---|
| superadmin@litxus.demo | Super Admin | install owner — full access including License + FeatureFlags |
| admin@litxus.demo | Admin | full business access, used for approvals |
| accountant@litxus.demo | Accountant | GL entry + reports testing *(not yet seeded — planned)* |
| sales@litxus.demo | SalesUser | invoice/customer testing *(not yet seeded — Phase 2)* |
| inventory@litxus.demo | InventoryManager | stock movement testing *(not yet seeded — Phase 3)* |
| manager@litxus.demo | Manager | read-only reports testing *(not yet seeded — planned)* |
| viewer@litxus.demo | Viewer | negative-permission testing, verify 403s *(not yet seeded — planned)* |

All seeded with password `Demo@12345` (Development/Demo environments only — never seeded in Production). `superadmin@litxus.demo` and `admin@litxus.demo` are implemented and verified (`UserSeeder`, [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.4); the remaining four are documented here as the target state but not yet built.

## 8.3 Realistic Malaysia Context

- **Company names:** e.g. "Tropikal Hardware Sdn Bhd", "Selangor Pipe Supplies Sdn Bhd", "KL Trading Enterprise", "Penang Distributors Sdn Bhd" — mix of Sdn Bhd (private limited) and Enterprise (sole prop) forms matching Companies Act 2016 entity types.
- **Currency:** All amounts MYR, displayed as `RM 1,250.00` (`en-MY` Intl.NumberFormat).
- **Phone numbers:** `+60 3-XXXX XXXX` (landline, KL), `+60 12-XXX XXXX` (mobile) formats.
- **Product categories:** plumbing/hardware-adjacent to reflect a plausible SME distributor (PVC pipes, fittings, tools, safety equipment) — chosen as a neutral, realistic vertical distinct from the unrelated PSMPE project.
- **Addresses:** real Malaysian states/cities (Selangor, Johor Bahru, Penang, Kuala Lumpur) without using real street addresses.

## 8.4 Test Scenario / Edge-Case Data

Seed data deliberately includes:
- One invoice past its due date with no payment (tests Overdue status + AR aging report)
- One invoice with a partial payment (tests PartiallyPaid status)
- One voided GL entry with a reason (tests audit trail display)
- One product below its reorder level (tests reorder alert)
- One customer at/over their credit limit (tests credit-limit warning on new invoice)
- One bank statement line with no matching GL entry (tests unreconciled-item report)

## 8.5 How Seeding Works (as actually built)

- `ISeeder` interface (`Order` + `SeedAsync`), implemented today by `RbacSeeder` (Order 1 — Permissions catalog from code, 7 roles, role→permission grants), `LicenseSeeder` (Order 2 — one local/demo Accounting Pro license), `UserSeeder` (Order 3 — `superadmin@litxus.demo` + `admin@litxus.demo`, via `UserManager<AppUser>` so passwords are hashed correctly, not inserted directly), `AccountingDemoDataSeeder` (Order 4 — 16 Chart of Accounts entries + 20 realistic posted GL entries spanning Jan–May, checked by `Account.Code` individually rather than "any accounts exist" so it layers on top of accounts a user already created by hand). Run in `Order` by `SeedDatabaseHostedService` on startup. Still smaller than the originally-planned 30-40 accounts / 100+ entries per §8.1 — enough to prove every report is correct, not the full target volume for pagination/performance testing.
- Gated by `appsettings.{Environment}.json` → `"Seeding": { "Enabled": true }` — **on** for Local and Demo environments, **off** by default in Production (a production install should never silently get demo data; if a customer wants sample data for training purposes, it's a deliberate, documented admin action).
- Idempotent: each seeder checks its own table is empty before inserting (`RbacSeeder` checks `Permissions`, `LicenseSeeder` checks `Licenses`, `UserSeeder` checks `AspNetUsers` via `UserManager.Users`), so re-running the app doesn't duplicate seed rows.
- **Verified**: fresh SQL Server container → migration → API startup → both `superadmin@litxus.demo` and `admin@litxus.demo` log in with the correct roles/permissions/enabledModules, and `admin@litxus.demo` successfully creates a GL account through the full RBAC-gated endpoint.
- Raw SQL scripts (`/docs/sample-data/*.sql`) are also generated from the seeders (via a `dotnet run --seed-export` utility) at the end of each phase, for cases where a customer wants to load sample data into a fresh DB without running the full app seeding pipeline (e.g. for a sales demo environment provisioned independently).

## 8.6 Performance Testing Data

For Phase 5 load testing, a separate `PerformanceSeeder` (never run outside a load-test environment) multiplies the above volumes ×10 (1,000+ GL entries, 200–300 invoices, 300–500 stock movements) to validate pagination, report query performance, and index effectiveness under realistic multi-year data volumes.
