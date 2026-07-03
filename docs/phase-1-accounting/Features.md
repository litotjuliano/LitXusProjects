# Phase 1 — LitXus Accounting Pro — Features

Duration: 4 weeks. Goal: a fully standalone, sellable Accounting product (GL, reports, tax, bank reconciliation) plus the RBAC/Audit/Identity foundation every later phase builds on.

---

## Feature 1: Account Bootstrap, Admin-Driven User Creation & Login
**Priority:** Must-have
**User story:** As the owner of a fresh install, I want the very first account I create to have full authority, and as an Admin, I want to create accounts for my staff directly, so nobody has to self-register or wait for a separate activation step.

**Description:** Email/password login using ASP.NET Identity + JWT. Self-registration (`POST /auth/register`) is bootstrap-only — it works exactly once, for the very first user on a fresh install, auto-activated and granted **Super Admin** (see Business_Rules.md). Every account after that is created directly by an Admin/Super Admin via **Administration → Users → + New User** (name, email, password, role in one step) — active immediately, no Pending state.

**Acceptance criteria:**
- [x] First-ever registration on a fresh install creates an active Super Admin account
- [x] Any later call to `/auth/register` is rejected
- [x] Admin/Super Admin can create a new user (name, email, password, role) in one step via the UI, active immediately
- [x] Role can be changed for an existing user via a dropdown on the Users page (calls the existing assign/revoke endpoints)
- [ ] Login rejects deactivated users with a clear message
- [x] Successful login returns access + refresh token pair and `GET /auth/me` payload (roles, permissions, enabled modules)
- [ ] Refresh token rotates on use; logout revokes it
- [ ] Password reset flow (request + confirm) works end-to-end

**Out of scope:** MFA, social login, 4-step wizard UI polish (basic single-step form is fine for Phase 1; the "4-step wizard" mentioned for the member-facing product in unrelated docs does not apply here).

---

## Feature 2: Roles & Permissions Management
**Priority:** Must-have
**User story:** As an Admin, I want to assign roles to users, so that each person only sees and does what their job requires.

**Acceptance criteria:**
- [x] 7 roles seeded: Super Admin, Admin, Accountant, SalesUser, InventoryManager, Manager, Viewer — Super Admin was added during implementation (not in the original 6-role plan) as the install owner tier with exclusive `Admin.License.*`/`Admin.FeatureFlags.*` access; see [06_RBAC_Auth.md](../06_RBAC_Auth.md) §6.2
- [x] Admin can view/assign/revoke roles per user — `GET/PATCH /admin/users`, `POST/DELETE /admin/users/{id}/roles`, live in the Users admin page
- [x] Admin can view the full permission catalog (read-only in Phase 1 — custom role creation is a later-phase nice-to-have) — `GET /admin/roles`, `GET /admin/permissions`, live in the Roles & Permissions admin page
- [x] Every mutating Accounting endpoint enforces its permission (`Accounting.{Entity}.{Operation}`)
- [x] Audit log viewer wired to real data (`GET /admin/audit-logs`) — this was originally scoped under Feature 8 but built alongside this feature since both needed the same admin-endpoint work

**Out of scope:** Custom role creation/editing UI (Phase 1 ships the 7 fixed roles; editable roles noted as a v1.1 candidate).

---

## Feature 3: Chart of Accounts
**Priority:** Must-have
**User story:** As an Accountant, I want to set up and maintain a chart of accounts, so that GL entries have somewhere valid to post to.

**Acceptance criteria:**
- [x] Create an account: code, name, type (Asset/Liability/Equity/Revenue/Expense), optional parent account; edit an existing account's name and/or parent (code and type are immutable once set — by design, not a gap)
- [x] Account codes are unique
- [x] Accounts can be deactivated (not deleted) and reactivated — deactivated accounts are excluded from the active-accounts dropdown used when creating new GL entries and hidden from the list by default, but remain visible in historical reports and via "Show inactive accounts"
- [x] Tree view groups accounts by parent/child hierarchy (indented, parent immediately followed by its children); reparenting into a circular relationship is rejected both client-side (excluded from the dropdown) and server-side (validated)

**Out of scope:** Multi-currency accounts (MYR only in v1.0).

---

## Feature 4: GL Entry Creation, Posting, Voiding
**Priority:** Must-have
**User story:** As an Accountant, I want to create, post, and (if needed) void journal entries, so that the ledger accurately reflects the business.

**Acceptance criteria:**
- [x] Create a Draft entry with 2+ lines, each a debit or credit to an active account
- [x] Draft entries are editable (date, description, and lines can all be replaced before posting); Posted entries are not
- [x] Posting requires balance (sum debits = sum credits) and updates account running balances — UI now shows the exact rejection reason (e.g. unbalanced amount) instead of failing silently
- [x] Voiding a Posted entry requires a reason (prompted in the UI), reverses the account balance impact, and never reuses the entry number
- [x] Entry numbers are sequential and gap-free (`JE-2026-000123`)

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
- [x] Trial Balance (as-of date) — computed from Posted GLEntryLines up to the date, not the denormalized Account.Balance field (which is always "now," not a historical point in time); verified balances to RM 0 against seeded demo data
- [x] Income Statement (date range) — verified Net Income matches Balance Sheet's Current Year Earnings exactly
- [x] Balance Sheet (as-of date) — no formal period-close exists yet (see [16 below](#) / Business_Rules.md), so it balances via a computed "Current Year Earnings" line (all-time Revenue − Expense) folded into Equity, the standard small-business-accounting shortcut for this
- [x] General Ledger detail (per account, date range) — includes a correctly-computed opening balance from activity before the range, and a running balance per line
- [x] CSV export — every report page has an "Export CSV" button that builds the file client-side from
  the already-fetched report data (no new backend endpoint; the API is Bearer-token-authenticated, not
  cookie-based, so a plain browser download link couldn't carry the auth header anyway). Verified live
  against seeded data for all 4 reports.
- [x] PDF export — a `GET .../pdf` sibling endpoint per report (`AccountingReportsController`) renders the
  already-fetched DTO via QuestPDF (Community license), with the company letterhead (name, address, SSM/TIN)
  matching the on-screen `ReportLetterhead`. Verified live: valid `%PDF-1.7` output for all 4 reports.
- [x] Excel export — a `GET .../excel` sibling endpoint per report renders to `.xlsx` via ClosedXML (MIT
  license). Verified live: valid OOXML (`PK` zip header) output for all 4 reports.

**Out of scope:** Cash flow statement, budget-vs-actual (v1.1 candidates).

---

## Feature 8: Audit Trail Viewer
**Priority:** Must-have
**User story:** As an Admin, I want to see a full history of who changed what, so that I can investigate discrepancies and satisfy compliance requirements.

**Acceptance criteria:**
- [x] Every GL entry/account/user/role/permission change is captured automatically (interceptor-based)
- [x] Admin can filter by entity, user, date range, action — `GET /admin/audit-logs` accepts `entityName`/`entityId`/`userId`/`action`/`dateFrom`/`dateTo`; the UI page itself doesn't expose filter controls yet, only the API does
- [x] Before/after values shown as a readable diff — expandable row with side-by-side JSON in the Audit Logs page
- [x] Audit rows are immutable (no edit/delete path exists anywhere in the app) — no UPDATE/DELETE MediatR command exists for AuditLog; DB-permission-level enforcement (§7.4) still to be validated as a Phase 5 checklist item

**Out of scope:** Automated archival tooling (manual process documented, not built, in Phase 1 — see [07_Audit_Trail.md](../07_Audit_Trail.md) §7.5).
