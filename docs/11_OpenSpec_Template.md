# 11 — OpenSpec Document Template

Every phase gets a `/docs/phase-N-{name}/` folder containing these seven files, written and reviewed **before** implementation starts. Below is the reusable template for each, followed by one fully worked example (GL Entry Posting, from Phase 1).

## 11.1 Features.md Template

```markdown
# Phase N — {Module} — Features

## Feature: {Name}
**Priority:** Must-have | Should-have | Nice-to-have
**User story:** As a {role}, I want to {action}, so that {benefit}.

**Description:** {1-2 paragraph plain-language description}

**Acceptance criteria:**
- [ ] {specific, testable condition}
- [ ] {specific, testable condition}

**Out of scope for this phase:** {explicit exclusions, so nobody assumes silent inclusion}
```

## 11.2 API_Specification.md Template

```markdown
## Endpoint: {METHOD} {path}
**Permission required:** {Module.Entity.Operation}
**Module required:** {Accounting | Sales | Inventory}

### Request
{JSON schema / example}

### Response — 200/201
{JSON schema / example}

### Errors
| Status | Code | When |
|---|---|---|
| 400 | VALIDATION_FAILED | {condition} |
| 422 | {SPECIFIC_CODE} | {business rule} |
```

## 11.3 Database_Schema.md Template

Table-by-table DDL (as in [02_Database_Schema.md](02_Database_Schema.md)) scoped to just this phase's new/changed tables, plus the migration name that will introduce them.

## 11.4 Business_Rules.md Template

```markdown
## Rule: {name}
**Statement:** {plain-language rule}
**Enforced at:** Domain entity method | FluentValidation | DB constraint
**Violation behavior:** {status code + message}
**Example:** {concrete before/after}
```

## 11.5 UI_Mockups.md Template

ASCII wireframe or described layout per screen, component list, states to design for (empty/loading/error/populated), and the primary user flow as a numbered sequence.

## 11.6 Test_Scenarios.md Template

Structured exactly per [09_Testing_Strategy.md](09_Testing_Strategy.md) §9.4 categories: Happy path / Error cases / Edge cases / Security — each a checklist of Given/When/Then statements.

## 11.7 Sample_Data.md Template

Table listing entity, row count, and any specific rows required to exercise the edge cases named in Test_Scenarios.md (cross-referenced by name).

---

## 11.8 Worked Example — Phase 1, Feature: GL Entry Posting

**Features.md excerpt:**
```markdown
## Feature: Post a GL Entry
Priority: Must-have
User story: As an Accountant, I want to post a draft GL entry, so that it becomes
part of the permanent ledger and affects account balances and reports.

Acceptance criteria:
- [ ] Only Draft entries can be posted
- [ ] Posting is rejected if total debits != total credits
- [ ] Posting is rejected if the entry has fewer than 2 lines
- [ ] On successful post: Status -> Posted, PostedAtUtc/PostedBy set, account balances updated
- [ ] Posting is audited as a distinct "Approve" action, not generic "Update"

Out of scope: posting to a closed accounting period (periods/period-close not in Phase 1 scope)
```

**API_Specification.md excerpt:**
```markdown
## Endpoint: POST /api/v1/accounting/gl-entries/{id}/post
Permission required: Accounting.GLEntry.Approve
Module required: Accounting

### Request
(no body)

### Response — 200
{ "data": { "id": "...", "status": "Posted", "postedAtUtc": "2026-07-01T09:00:00Z", "postedBy": "..." } }

### Errors
| Status | Code | When |
|---|---|---|
| 404 | NOT_FOUND | entry id doesn't exist |
| 422 | ENTRY_NOT_DRAFT | entry.status != Draft |
| 422 | ENTRY_UNBALANCED | sum(debits) != sum(credits) |
| 422 | ENTRY_TOO_FEW_LINES | line count < 2 |
```

**Business_Rules.md excerpt:**
```markdown
## Rule: GL entries must balance to post
Statement: Sum of DebitAmount across all lines must equal sum of CreditAmount before a GL entry
           can transition from Draft to Posted.
Enforced at: Domain entity method GLEntry.Post() — throws UnbalancedEntryException
Violation behavior: 422 ENTRY_UNBALANCED, message shows the actual debit/credit totals and the delta
Example: Lines totaling Dr 1,325.00 / Cr 1,250.00 -> rejected, message:
         "Entry is unbalanced by RM 75.00 (debit exceeds credit)."
```

**Test_Scenarios.md excerpt:**
```markdown
### Happy path
- Given a Draft entry with 2 balanced lines, When posted, Then status=Posted and both
  account balances reflect the amounts.

### Error cases
- Given an already-Posted entry, When post is called again, Then 422 ENTRY_NOT_DRAFT.

### Edge cases
- Given an entry with exactly 2 lines that balance to RM 0.00 each, When posted,
  Then it succeeds (a zero-value entry is technically balanced, though the UI warns
  before submission).

### Security
- Given a user without Accounting.GLEntry.Approve, When posting is attempted,
  Then 403.
```

This worked example is the pattern every subsequent feature in every phase follows — consistent enough that a new contributor can write an OpenSpec doc for a new feature without re-deriving the format each time.
