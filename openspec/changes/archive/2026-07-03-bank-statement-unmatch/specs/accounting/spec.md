## ADDED Requirements

### Requirement: A matched bank statement line can be unmatched
`POST /api/v1/accounting/bank-statement-lines/{id}/unmatch` (permission `Accounting.BankAccount.Update`) SHALL clear a `BankStatementLine`'s match, setting `IsReconciled` back to `false` and `MatchedGLEntryLineId` to `null`. Unmatching a line that isn't currently matched SHALL be rejected.

#### Scenario: Unmatching a reconciled line clears its match
- **WHEN** `Unmatch()` is called on a `BankStatementLine` whose `IsReconciled` is `true`
- **THEN** `IsReconciled` becomes `false` and `MatchedGLEntryLineId` becomes `null`, and the underlying GL entry line becomes eligible for matching again

#### Scenario: Unmatching an already-unmatched line is rejected
- **WHEN** `Unmatch()` is called on a `BankStatementLine` whose `IsReconciled` is already `false`
- **THEN** the request is rejected with `StatementLineNotMatchedException`: "This bank statement line isn't matched to anything yet."
