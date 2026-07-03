## Why

`BankAccount` and `BankStatementLine` (with a working `Match()`/`StatementLineAlreadyMatchedException` guard) already existed as fully-tested domain entities, and `docs/phase-1-accounting/API_Specification.md` already specified the exact endpoint contract — but no Application CQRS, controller, or frontend existed. `BankReconciliation.tsx` was a literal stub. Bank Reconciliation was the last unbuilt item in Phase 1's own feature checklist.

## What Changes

- New CQRS covering the full reconciliation workflow: list/create bank accounts, list statement lines, import a CSV of statement lines, match a statement line to a Posted GL entry line, list unmatched GL lines, get reconciliation status.
- New `BankAccountsController` and `BankStatementLinesController`, implementing the already-spec'd contract plus one new endpoint (`GET .../unmatched-gl-lines`) needed to populate the two-pane matching UI the mockup already describes.
- A CSV import format is designed here for the first time: `Date,Description,Amount` header, `yyyy-MM-dd` dates, signed amounts. Import is all-or-nothing — any malformed row rejects the whole file with every bad row listed.
- `BankReconciliation.tsx` rewritten from its stub into the real two-pane matching screen per `docs/phase-1-accounting/UI_Mockups.md`.
- Sample data: 2 bank accounts (Maybank, CIMB) linked to the already-seeded cash accounts, with statement lines derived from real Posted GL activity — mostly pre-matched, with a deliberately unmatched statement line and a couple of unmatched GL lines.

## Capabilities

### Modified Capabilities
- `accounting`: adds the bank-reconciliation workflow requirements around the already-spec'd "matched at most once" rule (baseline) — CSV import validation and cross-entity match eligibility (Posted-only, no double-matching a GL line) are new.

### New Capabilities
None.

## Impact

- Backend: `BankAccountDto`/`BankStatementLineDto`/`UnmatchedGLEntryLineDto`/`ReconciliationStatusDto`, `GetBankAccountsQuery`, `CreateBankAccountCommand`/Validator, `GetBankStatementLinesQuery`, `ImportBankStatementLinesCommand` + `BankStatementCsvParser`, `MatchBankStatementLineCommand`, `GetUnmatchedGLEntryLinesQuery`, `GetReconciliationStatusQuery`, `GLEntryLineAlreadyMatchedException`, `BankAccountsController`, `BankStatementLinesController`.
- Frontend: `helpers/api/bankReconciliation.ts`, rewritten `pages/accounting/BankReconciliation.tsx`.
- Seeding: `AccountingDemoDataSeeder.SeedBankAccountsAsync`.
- Tests: `BankStatementCsvParserTests.cs`.
- Docs: `docs/03_API_Specification.md` + `docs/phase-1-accounting/API_Specification.md` (new `unmatched-gl-lines` endpoint), `Features.md`, `User_Guide.md`, `08_Sample_Data.md`.
- No database schema changes — `BankAccounts`/`BankStatementLines` tables and their EF configurations already existed.
