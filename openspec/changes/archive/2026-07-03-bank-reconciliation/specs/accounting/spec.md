## ADDED Requirements

### Requirement: Bank statement lines can be imported via CSV
`POST /api/v1/accounting/bank-accounts/{id}/statement-lines/import` (permission `Accounting.BankAccount.Update`) SHALL accept a CSV file with header `Date,Description,Amount` (`yyyy-MM-dd` dates, signed decimal amounts) and create one `BankStatementLine` per valid row. Validation SHALL be all-or-nothing: if any row is malformed, the entire import SHALL be rejected with every malformed row's error listed, and no lines SHALL be persisted.

#### Scenario: A CSV with one malformed row rejects the whole import
- **WHEN** a CSV is submitted where row 3 has an unparseable date
- **THEN** the import is rejected with a `ValidationException` listing "Row 3" as one of the errors, and zero statement lines are created from that file

#### Scenario: A valid CSV imports every row
- **WHEN** a CSV with a valid header and 2 valid rows is submitted
- **THEN** 2 new `BankStatementLine` rows are created, unreconciled, for that bank account

### Requirement: Only a Posted GL entry line can be matched to a bank statement line, and only once
`POST /api/v1/accounting/bank-statement-lines/{id}/match` (permission `Accounting.BankAccount.Update`) SHALL only succeed if the target `GLEntryLine` belongs to a `Posted` `GLEntry` and is not already matched to a *different* statement line — in addition to the existing rule that the statement line itself must not already be reconciled.

#### Scenario: Matching to a Draft entry's line is rejected
- **WHEN** a match is attempted against a `GLEntryLineId` whose `GLEntry.Status` is `Draft`
- **THEN** the request is rejected with `NotFoundException`

#### Scenario: Matching a GL line already claimed by another statement line is rejected
- **WHEN** a `GLEntryLineId` is already referenced by a different `BankStatementLine.MatchedGLEntryLineId`
- **THEN** the request is rejected with `GLEntryLineAlreadyMatchedException`: "This GL entry line is already matched to another bank statement line."
