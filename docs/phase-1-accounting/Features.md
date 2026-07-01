# Phase 1 — LitXus Accounting Pro — Features

Duration: 4 weeks. Goal: a fully standalone, sellable Accounting product (GL, reports, tax, bank reconciliation) plus the RBAC/Audit/Identity foundation every later phase builds on.

---

## Feature 1: User Registration & Login
**Priority:** Must-have
**User story:** As a new customer admin, I want to register my company and log in, so that I can start using the system.

**Description:** Standard email/password registration and login using ASP.NET Identity + JWT. New registrations are created `Pending` and require an existing Admin to activate them (bootstrap: the very first user in a fresh install is auto-activated as Admin — see Business_Rules.md).

**Acceptance criteria:**
- [ ] Register with email, password, full name creates a `Pending` user
- [ ] Login rejects `Pending`/deactivated users with a clear message
- [ ] Successful login returns access + refresh token pair and `GET /auth/me` payload (roles, permissions, enabled modules)
- [ ] Refresh token rotates on use; logout revokes it
- [ ] Password reset flow (request + confirm) works end-to-end

**Out of scope:** MFA, social login, 4-step wizard UI polish (basic single-step form is fine for Phase 1; the "4-step wizard" mentioned for the member-facing product in unrelated docs does not apply here).

---

## Feature 2: Roles & Permissions Management
**Priority:** Must-have
**User story:** As an Admin, I want to assign roles to users, so that each person only sees and does what their job requires.

**Acceptance criteria:**
- [ ] 6 roles seeded: Admin, Accountant, SalesUser, InventoryManager, Manager, Viewer
- [ ] Admin can view/assign/revoke roles per user
- [ ] Admin can view the full permission catalog (read-only in Phase 1 — custom role creation is a later-phase nice-to-have)
- [ ] Every mutating Accounting endpoint enforces its permission (`Accounting.{Entity}.{Operation}`)

**Out of scope:** Custom role creation/editing UI (Phase 1 ships the 6 fixed roles; editable roles noted as a v1.1 candidate).

---

## Feature 3: Chart of Accounts
**Priority:** Must-have
**User story:** As an Accountant, I want to set up and maintain a chart of accounts, so that GL entries have somewhere valid to post to.

**Acceptance criteria:**
- [ ] Create/edit an account: code, name, type (Asset/Liability/Equity/Revenue/Expense), optional parent account
- [ ] Account codes are unique
- [ ] Accounts can be deactivated (not deleted) — deactivated accounts can't be used in new GL entries but remain visible in historical reports
- [ ] Tree view groups accounts by type and parent/child hierarchy

**Out of scope:** Multi-currency accounts (MYR only in v1.0).

---

## Feature 4: GL Entry Creation, Posting, Voiding
**Priority:** Must-have
**User story:** As an Accountant, I want to create, post, and (if needed) void journal entries, so that the ledger accurately reflects the business.

**Acceptance criteria:**
- [ ] Create a Draft entry with 2+ lines, each a debit or credit to an active account
- [ ] Draft entries are editable; Posted entries are not
- [ ] Posting requires balance (sum debits = sum credits) and updates account running balances
- [ ] Voiding a Posted entry requires a reason, reverses the account balance impact, and never reuses the entry number
- [ ] Entry numbers are sequential and gap-free (`JE-2026-000123`)

**Out of scope:** Recurring/template entries, multi-period allocation.

---

## Feature 5: SST Tax Handling
**Priority:** Must-have
**User story:** As an Accountant, I want SST calculated consistently, so that tax figures are correct and auditable.

**Acceptance criteria:**
- [ ] Tax codes (SST-6, SST-0) are seeded and configurable (rate stored as data)
- [ ] `POST /tax/calculate-sst` returns SST amount + total for a given subtotal and tax code
- [ ] Rounding is 2dp, away-from-zero, applied consistently

**Out of scope:** Full LHDN MyInvois submission (readiness only — see [15_Malaysia_Compliance.md](../15_Malaysia_Compliance.md)).

---

## Feature 6: Bank Reconciliation
**Priority:** Must-have
**User story:** As an Accountant, I want to match bank statement lines against GL entries, so that I can confirm the books agree with the bank.

**Acceptance criteria:**
- [ ] Create bank accounts linked to a GL cash/bank account
- [ ] Import statement lines via CSV
- [ ] Manually match a statement line to a GL entry line (one-to-one in Phase 1)
- [ ] Reconciliation status view shows matched vs. unmatched lines per bank account

**Out of scope:** Automatic/fuzzy matching (manual match only in Phase 1), multi-line matching (one statement line ↔ many GL lines).

---

## Feature 7: Financial Reports
**Priority:** Must-have
**User story:** As an Accountant or Manager, I want standard financial reports, so that I can review the company's financial position.

**Acceptance criteria:**
- [ ] Trial Balance (as-of date) — always sums to zero against valid data
- [ ] Income Statement (date range)
- [ ] Balance Sheet (as-of date)
- [ ] General Ledger detail (per account, date range)
- [ ] All reports exportable as PDF, Excel, CSV

**Out of scope:** Cash flow statement, budget-vs-actual (v1.1 candidates).

---

## Feature 8: Audit Trail Viewer
**Priority:** Must-have
**User story:** As an Admin, I want to see a full history of who changed what, so that I can investigate discrepancies and satisfy compliance requirements.

**Acceptance criteria:**
- [ ] Every GL entry/account/user/role/permission change is captured automatically (interceptor-based)
- [ ] Admin can filter by entity, user, date range, action
- [ ] Before/after values shown as a readable diff
- [ ] Audit rows are immutable (no edit/delete path exists anywhere in the app)

**Out of scope:** Automated archival tooling (manual process documented, not built, in Phase 1 — see [07_Audit_Trail.md](../07_Audit_Trail.md) §7.5).
