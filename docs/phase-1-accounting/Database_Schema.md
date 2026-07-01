# Phase 1 — Database Schema

All tables below are the Phase 1 subset of [02_Database_Schema.md](../02_Database_Schema.md) — reproduced here scoped to this phase so the OpenSpec doc is self-contained for implementation. Full detail (indexes, ER diagram) lives in the master doc; this file is the authoritative list of what Phase 1's migration must create.

## Migration: `20260706_Phase1_IdentityAndAccountingSchema`

**Identity / Shared (from §2.1):**
- `AspNetUsers` (extended: `FullName`, `IsActive`, `LastLoginAtUtc`) + standard Identity tables
- `Roles`, `Permissions`, `RolePermissions`, `UserRoles`
- `AuditLogs`
- `Licenses` (seeded with a single row for the local dev instance; `EnabledModules = "Accounting"`)
- `Notifications` (table created now since it's shared infra; UI for it is not a Phase 1 deliverable)

**Accounting (from §2.2):**
- `Accounts`
- `GLEntries`
- `GLEntryLines`
- `TaxCodes`
- `BankAccounts`
- `BankStatementLines`

## Phase 1 Seed Data (schema-level, not row volumes — see [Sample_Data.md](Sample_Data.md))

- 6 `Roles` rows: Admin, Accountant, SalesUser, InventoryManager, Manager, Viewer
- Full `Permissions` catalog generated from the `Module`/`Entity`/`Operation` enum in code (not hand-typed — avoids drift)
- `RolePermissions` per the matrix in [06_RBAC_Auth.md](../06_RBAC_Auth.md) §6.2
- 2 `TaxCodes`: SST-6 (6.00%), SST-0 (0.00%)
- 1 `Licenses` row for local/demo: `ProductCode=AccountingPro`, `EnabledModules=Accounting`

## Indexes Required for Phase 1 (subset of §2.6)

```sql
CREATE INDEX IX_GLEntries_EntryDate        ON GLEntries(EntryDate);
CREATE INDEX IX_GLEntries_Status           ON GLEntries(Status);
CREATE INDEX IX_GLEntryLines_AccountId     ON GLEntryLines(AccountId);
CREATE INDEX IX_AuditLogs_EntityName_EntityId ON AuditLogs(EntityName, EntityId);
CREATE INDEX IX_AuditLogs_TimestampUtc     ON AuditLogs(TimestampUtc);
CREATE INDEX IX_AuditLogs_UserId           ON AuditLogs(UserId);
CREATE UNIQUE INDEX UX_Accounts_Code       ON Accounts(Code);
CREATE UNIQUE INDEX UX_GLEntries_EntryNumber ON GLEntries(EntryNumber) WHERE EntryNumber IS NOT NULL;
```

Note: `EntryNumber` is nullable until posted (Draft entries have no number yet), so the unique index is filtered (`WHERE EntryNumber IS NOT NULL`) — this is a Phase 1-specific detail not spelled out in the master schema doc, worth calling out since it affects the EF Core Fluent API configuration (`HasIndex(...).IsUnique().HasFilter(...)`).

## EF Core Entity Configuration Notes

- `GLEntryLine`: `CHECK (DebitAmount = 0 OR CreditAmount = 0)` — implement via `HasCheckConstraint` in `OnModelCreating`, not just app-level validation, since this is a financial integrity invariant worth enforcing at the DB layer too.
- `Account`: self-referencing FK (`ParentAccountId -> Accounts.Id`), `ON DELETE NO ACTION` (SQL Server would otherwise reject a self-referencing cascade path anyway).
- `AuditLog`: no `IAuditable` interception on itself (would recurse) — it's written directly by the interceptor/`IAuditLogger`, never by a tracked-entity save.
