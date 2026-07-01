# Phase 1 — Test Scenarios

Structured per [09_Testing_Strategy.md](../09_Testing_Strategy.md) §9.4 categories. Given/When/Then format; each maps to a unit or integration test.

## Auth & RBAC

### Happy path
- Given a fresh install with no users, When the first `/auth/register` call succeeds, Then the user is `IsActive=true` with Admin role.
- Given a Pending user, When an Admin calls `PATCH /admin/users/{id}/status { isActive: true }`, Then the user can subsequently log in.
- Given valid credentials, When `/auth/login` is called, Then a valid access + refresh token pair is returned and `/auth/me` reflects correct roles/permissions/enabledModules.
- Given an expired access token, When a request is retried after `/auth/refresh`, Then it succeeds without the user re-entering credentials.

### Error cases
- Given a second registration on a non-empty install, When submitted, Then user is created `Pending` (not auto-Admin).
- Given wrong password, When `/auth/login` is called, Then 401, generic "invalid credentials" (no user-exists disclosure).
- Given a Pending or deactivated user, When `/auth/login` is called with correct credentials, Then 403 `USER_NOT_ACTIVE`.

### Edge cases
- Given a refresh token that was already used once, When reused, Then rejected (rotation/replay detection) and both tokens invalidated.
- Given a JWT signed with a different key (tampered), When used, Then 401.

### Security
- Given a Viewer role, When any mutating Accounting endpoint is called, Then 403.
- Given no Authorization header, When any non-auth endpoint is called, Then 401.

## Chart of Accounts

### Happy path
- Given valid unique code/name/type, When `POST /accounting/accounts`, Then 201 and account appears in tree view under correct parent.
- Given an existing account, When deactivated, Then it's excluded from the GL entry account picker but still visible in historical reports.

### Error cases
- Given a duplicate code, When `POST /accounting/accounts`, Then 409 `ACCOUNT_CODE_DUPLICATE`.
- Given an attempt to change `code` via `PUT`, Then the field is ignored/rejected, code remains unchanged.

### Edge cases
- Given a 3-level parent/child/grandchild account hierarchy, When the tree view renders, Then nesting displays correctly and doesn't infinite-loop on a (disallowed) circular parent reference.

## GL Entries

### Happy path
- Given a Draft entry with 2 balanced lines, When posted, Then status=Posted, EntryNumber assigned, account balances updated.
- Given a Posted entry, When voided with a reason, Then status=Voided, account balances reversed, EntryNumber unchanged and not reused by any future entry.

### Error cases
- Given an unbalanced entry (Dr ≠ Cr), When post is attempted, Then 422 `ENTRY_UNBALANCED` with the correct delta amount in the message.
- Given a single-line entry, When post is attempted, Then 422 `ENTRY_TOO_FEW_LINES`.
- Given an already-Posted entry, When `PUT` (edit) is attempted, Then 422 `ENTRY_NOT_DRAFT`.
- Given a void request with no reason, When submitted, Then 400 `VOID_REQUIRES_REASON`.
- Given a line referencing a deactivated account, When creating/posting, Then 422 `ACCOUNT_INACTIVE`.

### Edge cases
- Given a 2-line entry where both lines are RM 0.00, When posted, Then it succeeds (technically balanced) — verify no divide-by-zero or false-negative in the balance check.
- Given 50 concurrent `POST /gl-entries/{id}/post` calls across 50 different Draft entries, When all execute simultaneously, Then all 50 receive distinct, sequential, gap-free entry numbers (this is the concurrency-safety test called out as a key risk in [13_Roadmap.md](../13_Roadmap.md)).
- Given an entry dated in the future, When created, Then accepted (Phase 1 has no period-close restriction — documented as in-scope-for-now, not a bug).

### Security
- Given an Accountant role (has Create+Update, not Approve per the seeded matrix), When posting is attempted, Then 403.
- Given a SalesUser role, When any GL entry endpoint is called, Then 403 (no Accounting permissions granted).

## Tax / SST

### Happy path
- Given subTotal=1250.00 and SST-6, When `/tax/calculate-sst` is called, Then sstAmount=75.00, total=1325.00.

### Edge cases
- Given subTotal=0.005 boundary rounding cases, When calculated, Then rounding follows away-from-zero at 2dp consistently (table-driven test with several boundary inputs).

## Bank Reconciliation

### Happy path
- Given an imported CSV of statement lines, When uploaded, Then all rows appear as unmatched.
- Given a statement line and a GL entry line, When matched, Then both show as reconciled and reconciliation status count updates.

### Error cases
- Given an already-matched statement line, When matched again, Then 409 `STATEMENT_LINE_ALREADY_MATCHED`.
- Given a malformed CSV (missing required column), When imported, Then 400 with a clear message identifying the problem row/column.

## Reports

### Happy path
- Given the Phase 1 seed data, When Trial Balance is generated as-of the seed data's latest date, Then total debits = total credits exactly.
- Given a date range spanning multiple Posted and Voided entries, When Income Statement is generated, Then Voided entries are excluded from the totals.

### Edge cases
- Given zero GL entries (fresh install, no seed data), When any report is generated, Then it returns an empty-but-valid result (zeros), not an error.
- Given 1,000+ GL entries (performance seed), When General Ledger detail is requested for one account, Then response time stays under 2s (ties to [09_Testing_Strategy.md](../09_Testing_Strategy.md) §9.5 performance targets).

## Audit Trail

### Happy path
- Given a GL entry create → post → void lifecycle, When audit logs are queried by EntityId, Then exactly 3 rows appear in chronological order with correct Action values (Create, Approve, Void).
- Given a role permission change, When performed, Then an audit row captures who changed what for whom.

### Security
- Given a non-Admin user, When `GET /admin/audit-logs` is attempted, Then 403.
- Given direct SQL access with the app's connection credential, When an `UPDATE AuditLogs ...` is attempted, Then it's rejected at the DB permission level (manual DBA-level test, documented as a Phase 5 compliance checklist item too).
