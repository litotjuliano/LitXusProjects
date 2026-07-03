## 1. Backend

- [x] 1.1 Add `BankAccountDto`/`BankStatementLineDto`/`UnmatchedGLEntryLineDto`/`ReconciliationStatusDto`
- [x] 1.2 Add `GetBankAccountsQuery`/Handler, `CreateBankAccountCommand`/Handler/Validator
- [x] 1.3 Add `GetBankStatementLinesQuery`/Handler
- [x] 1.4 Add `BankStatementCsvParser` (hand-rolled, quoted-field support) + `ImportBankStatementLinesCommand`/Handler
- [x] 1.5 Add `MatchBankStatementLineCommand`/Handler + `GLEntryLineAlreadyMatchedException` (409)
- [x] 1.6 Add `GetUnmatchedGLEntryLinesQuery`/Handler, `GetReconciliationStatusQuery`/Handler
- [x] 1.7 Add `BankAccountsController`, `BankStatementLinesController`

## 2. Frontend

- [x] 2.1 Add `helpers/api/bankReconciliation.ts`
- [x] 2.2 Rewrite `pages/accounting/BankReconciliation.tsx` — bank account selector, CSV import, two-pane click-to-select matching, reconciliation status

## 3. Sample Data

- [x] 3.1 `AccountingDemoDataSeeder.SeedBankAccountsAsync` — 2 bank accounts, statement lines derived from real Posted GL activity, deliberately unmatched lines on both sides

## 4. Tests

- [x] 4.1 `BankStatementCsvParserTests.cs` — valid parse, quoted comma, wrong header, malformed row rejection, empty file

## 5. Documentation

- [x] 5.1 OpenSpec accounting spec (this change)
- [x] 5.2 `docs/03_API_Specification.md` + `docs/phase-1-accounting/API_Specification.md` — new `unmatched-gl-lines` endpoint
- [x] 5.3 `docs/phase-1-accounting/Features.md` — check off Bank Reconciliation
- [x] 5.4 `docs/phase-1-accounting/User_Guide.md` — new scenario
- [x] 5.5 `docs/08_Sample_Data.md` — bank accounts no longer "deferred"

## 6. Verification

- [x] 6.1 Backend + frontend build clean, full test suite passes
- [x] 6.2 Live: switch bank accounts, import a CSV (valid + malformed), select + match a pair, confirm status updates
- [x] 6.3 Confirm Viewer gets 403 creating a bank account, 200 reading
