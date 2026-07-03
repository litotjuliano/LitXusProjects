## 1. Backend

- [x] 1.1 `BankStatementLine.Unmatch()` + `StatementLineNotMatchedException`
- [x] 1.2 `UnmatchBankStatementLineCommand`/Handler
- [x] 1.3 `POST .../bank-statement-lines/{id}/unmatch` on `BankStatementLinesController`

## 2. Frontend

- [x] 2.1 `unmatchStatementLine()` helper
- [x] 2.2 "Unmatch" button on matched rows in `BankReconciliation.tsx`

## 3. Documentation

- [x] 3.1 OpenSpec accounting spec (this change)
- [x] 3.2 API spec docs — new endpoint
- [x] 3.3 User_Guide.md — mention Unmatch in the Bank Reconciliation scenario

## 4. Verification

- [x] 4.1 Backend + frontend build clean, full test suite passes
- [x] 4.2 Live: unmatch a matched line, confirm reconciliation status count decreases and the line reappears as unmatched
