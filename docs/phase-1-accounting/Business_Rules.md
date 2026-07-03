# Phase 1 — Business Rules

## Rule: Self-registration is bootstrap-only — first user becomes Super Admin, every call after that is rejected
**Statement:** If `AspNetUsers` is empty when `/auth/register` is called, the new user is created `IsActive=true` and auto-assigned the **Super Admin** role — they're the literal install owner and need license authority immediately (a production install has no seeded accounts at all, and no other account could grant them Super Admin afterward, since self-escalation is deliberately blocked). Every subsequent call to `/auth/register` is rejected outright — there is no Pending/self-service path anymore. All other users are created directly by an Admin/Super Admin via `POST /api/v1/admin/users` (see `docs/phase-1-accounting/Admin_Setup_User_Guide.md` §3), active immediately with a role already assigned, no separate activation step.
**Enforced at:** `IdentityService.RegisterAsync` (Infrastructure layer — auth is a direct service, not a MediatR command), checked via `userManager.Users.AnyAsync()` before creation.
**Violation behavior:** A second (or later) call to `/auth/register` throws `AuthenticationException("Self-registration is disabled. Ask an administrator to create your account.")`, surfaced as 400.
**Example:** Fresh install, first `POST /auth/register` → user is immediately usable as Super Admin. Second call to `POST /auth/register` (by anyone, at any time after) → rejected. An Admin instead calls `POST /admin/users` with the new person's details and a role — that account is active and role-assigned immediately.

## Rule: GL entries must balance to post
**Statement:** Sum of `DebitAmount` across all lines must equal sum of `CreditAmount` before a GL entry can transition Draft → Posted.
**Enforced at:** `GLEntry.Post()` domain method — throws `UnbalancedEntryException`.
**Violation behavior:** 422 `ENTRY_UNBALANCED`, message includes the actual delta.
**Example:** Lines totaling Dr 3,500.00 / Cr 3,425.00 → rejected: "Entry is unbalanced by RM 75.00 (debit exceeds credit)."

## Rule: GL entries need at least 2 lines to post
**Statement:** A single-line entry cannot be balanced by definition and is rejected on post.
**Enforced at:** `GLEntry.Post()`.
**Violation behavior:** 422 `ENTRY_TOO_FEW_LINES`.

## Rule: Only Draft entries are editable
**Statement:** Once an entry is Posted or Voided, its lines, date, and description are immutable. Corrections happen via a new reversing/adjusting entry, never by editing history.
**Enforced at:** `PUT /gl-entries/{id}` handler checks `entry.Status == Draft` before allowing changes; also enforced in `GLEntry.UpdateLines()` domain method.
**Violation behavior:** 422 `ENTRY_NOT_DRAFT`.

## Rule: Voiding requires a reason and never reuses the entry number
**Statement:** `POST /gl-entries/{id}/void` requires a non-empty `reason`. The voided entry keeps its `EntryNumber` permanently (satisfies the gap-free/non-reused numbering requirement in [15_Malaysia_Compliance.md](../15_Malaysia_Compliance.md) §15.6) — it is simply excluded from balance calculations going forward.
**Enforced at:** `VoidGLEntryCommandHandler` + FluentValidation on the request.
**Violation behavior:** 400 `VOID_REQUIRES_REASON` if reason missing; 422 `ENTRY_NOT_DRAFT`-equivalent (`ENTRY_ALREADY_VOIDED` or must be Posted) if entry isn't in a voidable state.

## Rule: Entry numbers are sequential and gap-free
**Statement:** `EntryNumber` (`JE-YYYY-NNNNNN`) is assigned only at posting time (not at creation, since Draft entries may never be posted), using a DB-level sequence/serializable transaction — never `MAX(number) + 1` in application code, which is unsafe under concurrent posting.
**Enforced at:** `GLEntry.Post()` calls `INumberSequenceGenerator.NextGLEntryNumber()`, implemented via a SQL Server `SEQUENCE` object or `sp_getapplock`-guarded increment.
**Violation behavior:** N/A — this is a correctness guarantee, tested via concurrent-post integration tests (see Test_Scenarios.md).

## Rule: GL lines must reference active accounts
**Statement:** A deactivated `Account` cannot be used in a new or edited Draft GL entry line.
**Enforced at:** FluentValidation validator for `CreateGLEntryCommand`/`UpdateGLEntryCommand`, checking `Account.IsActive`.
**Violation behavior:** 422 `ACCOUNT_INACTIVE`.

## Rule: Account codes are unique and immutable after creation
**Statement:** `Accounts.Code` is unique; once set, it cannot be changed via `PUT /accounts/{id}` (only `Name` and `ParentAccountId` are editable) — codes are referenced externally (statutory filings, prior-year comparisons) and must be stable.
**Enforced at:** DB unique index + FluentValidation (silently ignores/rejects `code` field on update requests).
**Violation behavior:** 409 `ACCOUNT_CODE_DUPLICATE` on create.

## Rule: SST rounding is 2dp, away-from-zero
**Statement:** `SstAmount = Round(SubTotal * Rate/100, 2, AwayFromZero)`.
**Enforced at:** `SstCalculator.Calculate()` — single source of truth, referenced from both the standalone `/tax/calculate-sst` endpoint and (in later phases) Sales invoice line calculation.
**Violation behavior:** N/A (calculation rule, not a rejection).

## Rule: Bank statement lines can only be matched once
**Statement:** A `BankStatementLine` with `IsReconciled = true` cannot be matched again without first being unmatched.
**Enforced at:** `MatchStatementLineCommandHandler`.
**Violation behavior:** 409 `STATEMENT_LINE_ALREADY_MATCHED`.

## Rule: Deactivated users cannot log in
**Statement:** `IsActive = false` (includes still-`Pending` users) blocks `/auth/login` regardless of correct credentials.
**Enforced at:** `LoginCommandHandler`, checked immediately after password validation.
**Violation behavior:** 403 `USER_NOT_ACTIVE`.

## Rule: Every permission check is server-enforced, never UI-only
**Statement:** Frontend hiding of buttons/routes is UX only. Every mutating endpoint independently checks `[RequirePermission]` and `[RequireModule]` regardless of what the frontend renders.
**Enforced at:** Action filters on every controller action (see [06_RBAC_Auth.md](../06_RBAC_Auth.md) §6.5).
**Violation behavior:** 403, generic "You do not have permission to perform this action" (deliberately not revealing which specific permission is missing, to avoid leaking the permission model to a probing client).
