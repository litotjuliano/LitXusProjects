# accounting Specification

## Purpose
TBD - created by archiving change baseline-existing-system. Update Purpose after archive.
## Requirements
### Requirement: GL entries must balance and have at least two lines
`GLEntry` SHALL reject posting unless total debits equal total credits across its lines, and SHALL require at least 2 lines. Each line MUST have either a debit or a credit amount, never both, and amounts MUST be non-negative.

#### Scenario: Unbalanced entry cannot be posted
- **WHEN** a GL entry's lines sum to RM 100.00 in debits and RM 90.00 in credits
- **THEN** posting is rejected with `EntryUnbalancedException`: "Entry is unbalanced by RM 10.00 (debit exceeds the other side)."

#### Scenario: Single-line entry cannot be posted
- **WHEN** a GL entry has only 1 line
- **THEN** posting is rejected with `EntryTooFewLinesException`: "A GL entry needs at least 2 lines to be posted."

#### Scenario: A line cannot carry both a debit and a credit
- **WHEN** a line is submitted with both `DebitAmount` and `CreditAmount` greater than zero
- **THEN** the create-entry request is rejected by validation

### Requirement: GL entries follow a Draft → Posted → Voided lifecycle
Only `Draft` entries SHALL be editable or postable. Voiding a `Posted` entry SHALL require a non-empty reason and SHALL reverse the entry's balance impact (debits become credits and vice versa on the affected accounts) rather than deleting it.

#### Scenario: Editing a Posted entry is rejected
- **WHEN** an update is attempted on a GL entry whose `Status` is `Posted`
- **THEN** the system rejects it with `EntryNotDraftException`

#### Scenario: Voiding without a reason is rejected
- **WHEN** a void request is submitted with an empty or whitespace-only reason
- **THEN** the system rejects it with `VoidRequiresReasonException`

#### Scenario: Posting assigns a sequential entry number
- **WHEN** a Draft entry is posted
- **THEN** it receives the next sequential, gap-free entry number from `INumberSequenceGenerator` (numbers are never reused)

### Requirement: Accounts have immutable codes and a fixed debit/credit normal balance by type
Once created, an `Account`'s `Code` SHALL be immutable and unique. Asset and Expense accounts SHALL be debit-normal (credits reduce their balance); Liability, Equity, and Revenue accounts SHALL be credit-normal (debits reduce their balance). Accounts SHALL only be deactivated (`SetActive(false)`), never deleted, and posting is rejected against an inactive account.

#### Scenario: Creating an account with a duplicate code is rejected
- **WHEN** an account is created with a `Code` that already exists
- **THEN** the system rejects it with `AccountCodeDuplicateException`

#### Scenario: Posting against an inactive account is rejected
- **WHEN** a GL entry line references an account whose `IsActive` is `false`
- **THEN** posting is rejected with `AccountInactiveException`

### Requirement: Tax is calculated with 2-decimal-place away-from-zero rounding
`TaxCode` (type `Sst` or `IncomeTax`) SHALL compute tax as `subTotal * (Rate / 100)`, rounded to 2 decimal places using `MidpointRounding.AwayFromZero`, per Malaysian tax compliance conventions.

#### Scenario: Half-cent rounds away from zero
- **WHEN** a tax calculation yields exactly RM 0.125
- **THEN** the result rounds to RM 0.13, not RM 0.12

### Requirement: Bank statement lines can be matched to a GL entry line at most once
A `BankStatementLine` SHALL only be reconciled by matching it to one `GLEntryLine`; re-matching an already-matched line SHALL be rejected.

#### Scenario: Re-matching an already-reconciled statement line is rejected
- **WHEN** a `Match(glEntryLineId)` is attempted on a statement line whose `IsReconciled` is already `true`
- **THEN** the system rejects it with `StatementLineAlreadyMatchedException`

### Requirement: Financial reports only include Posted entries
Trial Balance, Income Statement, Balance Sheet, and General Ledger SHALL exclude Draft and Voided entries — only entries with `Status == Posted` contribute to any reported balance.

#### Scenario: A Draft entry does not appear in the Trial Balance
- **WHEN** a Draft GL entry exists alongside Posted entries
- **THEN** the Trial Balance's account totals reflect only the Posted entries

#### Scenario: Balance Sheet folds current-year earnings into Equity
- **WHEN** the Balance Sheet is generated as of a given date
- **THEN** "Current Year Earnings" (all-time Revenue minus all-time Expense through that date) is computed and included within Equity, since there is no formal period-close step

#### Scenario: Income Statement and General Ledger are period-bound, Balance Sheet and Trial Balance are as-of-date
- **WHEN** an Income Statement or General Ledger is requested with a From/To date range
- **THEN** only entries with `EntryDate` inclusively between From and To are included, whereas Trial Balance and Balance Sheet use a single as-of date covering all Posted entries up to and including it

### Requirement: Tax codes can be listed and created via the API
`GET /api/v1/accounting/tax-codes` (permission `Accounting.TaxCode.Read`) SHALL return all tax codes. `POST /api/v1/accounting/tax-codes` (permission `Accounting.TaxCode.Create`) SHALL create a new tax code, rejecting a duplicate `Code` with `TaxCodeDuplicateException`.

#### Scenario: Creating a tax code with a duplicate code is rejected
- **WHEN** a tax code is created with a `Code` that already exists
- **THEN** the request is rejected with `TaxCodeDuplicateException`: "A tax code with code '{code}' already exists."

### Requirement: SST is calculated via a dedicated calculator service, exposed through the API
`POST /api/v1/accounting/tax/calculate-sst` (permission `Accounting.TaxCode.Read`) SHALL accept a sub-total and a `taxCodeId`, and return the tax amount and total computed via `ISstCalculator`, which SHALL delegate to `TaxCode.Calculate()`'s existing 2dp away-from-zero rounding rule rather than reimplementing it.

#### Scenario: Calculating SST for an unknown tax code is rejected
- **WHEN** `calculate-sst` is called with a `taxCodeId` that doesn't exist
- **THEN** the request is rejected with `NotFoundException`

#### Scenario: Calculator result matches the entity's own rounding
- **WHEN** `calculate-sst` is called with subTotal RM 2.50 against a 1%-rate tax code
- **THEN** the SST amount returned is RM 0.03, matching `TaxCode.Calculate(2.50m)` directly (away-from-zero rounding of the exact RM 0.025 midpoint)

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

### Requirement: A matched bank statement line can be unmatched
`POST /api/v1/accounting/bank-statement-lines/{id}/unmatch` (permission `Accounting.BankAccount.Update`) SHALL clear a `BankStatementLine`'s match, setting `IsReconciled` back to `false` and `MatchedGLEntryLineId` to `null`. Unmatching a line that isn't currently matched SHALL be rejected.

#### Scenario: Unmatching a reconciled line clears its match
- **WHEN** `Unmatch()` is called on a `BankStatementLine` whose `IsReconciled` is `true`
- **THEN** `IsReconciled` becomes `false` and `MatchedGLEntryLineId` becomes `null`, and the underlying GL entry line becomes eligible for matching again

#### Scenario: Unmatching an already-unmatched line is rejected
- **WHEN** `Unmatch()` is called on a `BankStatementLine` whose `IsReconciled` is already `false`
- **THEN** the request is rejected with `StatementLineNotMatchedException`: "This bank statement line isn't matched to anything yet."

