## Why

`docs/phase-1-accounting/UI_Mockups.md`'s reconciliation screen and the bank-reconciliation change's own design notes flagged that a wrong match couldn't be corrected — `BankStatementLine.Match()` rejects re-matching an already-reconciled line with no way to undo it first. A real reconciliation workflow needs to correct mistakes.

## What Changes

- New `BankStatementLine.Unmatch()` domain method (clears `MatchedGLEntryLineId`, flips `IsReconciled` back to `false`), rejecting an unmatch attempt on a line that isn't currently matched.
- New `POST /api/v1/accounting/bank-statement-lines/{id}/unmatch` endpoint.
- New "Unmatch" button on each matched row in `BankReconciliation.tsx`.

## Capabilities

### Modified Capabilities
- `accounting`: adds the unmatch requirement alongside the existing "matched at most once" rule.

## Impact

- Backend: `StatementLineNotMatchedException`, `UnmatchBankStatementLineCommand`/Handler, new endpoint on `BankStatementLinesController`.
- Frontend: `unmatchStatementLine()` helper, Unmatch button + `unmatchingId` busy-state.
- No database schema changes.
