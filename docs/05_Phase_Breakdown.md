# 05 — Phase-by-Phase Detailed Breakdown

Each phase follows the same workflow: **OpenSpec → Implement → Test → Document → Release** (see [11_OpenSpec_Template.md](11_OpenSpec_Template.md)). No phase's implementation starts before its OpenSpec docs are written and reviewed.

---

## Phase 1 — LitXus Accounting Pro (4 weeks, ~20 working days)

**Goal:** Fully standalone Accounting module — this alone is a sellable product.

**Backend features:**
- Chart of Accounts CRUD (hierarchical, parent/child accounts)
- GL entry creation, posting (balance validation), voiding
- SST tax code setup + calculation service
- Bank account + statement import (CSV) + manual reconciliation matching
- Reports: Trial Balance, Income Statement, Balance Sheet, General Ledger detail
- RBAC + Identity foundation (used by all later phases)
- Audit trail foundation (interceptor, AuditLogs table, admin viewer)

**Frontend components:**
- Login/register pages, protected route shell, nav with module guard
- Chart of Accounts tree view + create/edit modal
- GL Entry list (filterable) + entry form (multi-line debit/credit editor with running balance check)
- Reports pages with date-range pickers, export buttons (PDF/Excel/CSV)
- Bank reconciliation screen (two-pane: statement lines vs. unmatched GL lines)
- Admin: Users, Roles, Audit Log viewer

**Database:** All Accounting + all Shared/Identity tables from [02_Database_Schema.md](02_Database_Schema.md) §2.1–2.2.

**API:** 20+ endpoints, §3.3–3.5 of [03_API_Specification.md](03_API_Specification.md).

**Sample data:** 30–40 CoA accounts, 100+ GL entries (mix of draft/posted/voided), 2–3 bank accounts with statement lines, 6 seeded user roles.

**Testing checklist:**
- [ ] Unit: GL entry balance validation (dr=cr), account balance calc, SST calculation, sequential numbering never skips
- [ ] Integration: full CRUD round-trip per endpoint, 401/403 for unauthenticated/unauthorized calls
- [ ] Manual: create → post → void GL entry lifecycle; reconciliation matching; report accuracy against known seed data
- [ ] Security: role X cannot post GL entries without `Accounting.GLEntry.Create` permission
- [ ] Edge cases: unbalanced entry rejected, voiding a reconciled entry blocked, negative balances flagged

**Documentation:** Swagger live, user guide (screenshots: login, CoA, GL entry, reports), dev guide (architecture, how to add a new report), Phase 1 completion report.

**Estimated effort:** ~20 days (backend 11, frontend 7, testing/docs 2, overlapping where sensible).

**Success criteria / sign-off:** All Phase 1 OpenSpec test scenarios pass; >80% unit coverage on Domain+Application; trial balance always balances to zero against seed data; no critical/high security findings.

---

## Phase 2 — LitXus Retail Pro, Part 1: Sales (3 weeks, ~15 days)

**Backend features:** Customer CRUD, Invoice CRUD + lifecycle (Draft→Issued→Paid/Void), sequential invoice numbering (gap-free, same pattern as GL entries), payment recording + admin verification, credit notes, sales reports (summary, AR aging).

**Frontend components:** Customers list/form, Invoice list + detail + line editor, "Issue Invoice" action with confirmation, Payment modal, Payment verification queue (admin), Sales reports dashboard with Recharts.

**Database:** Sales tables, §2.3.

**API:** 15+ endpoints, §3.6.

**Sample data:** 30–50 Malaysian customers, 20–30 invoices across statuses, 15–20 payments, 2–3 credit notes.

**Testing checklist:**
- [ ] Unit: invoice numbering gap-free under concurrent creation, SST calc per line, status transition guards (can't edit Issued invoice)
- [ ] Integration: invoice → payment → status update flow end-to-end
- [ ] Manual: full happy path (create customer → create invoice → issue → record payment → admin verifies → status becomes Paid)
- [ ] Edge cases: overpayment rejected, voiding invoice with verified payment blocked, credit note exceeding invoice total rejected
- [ ] Security: Sales User role cannot verify payments (Admin-only permission)

**Documentation:** Updated Swagger, user guide additions, Phase 2 completion report.

**Estimated effort:** ~15 days.

**Success criteria:** Sales module functions fully standalone (Accounting module can be disabled and Sales still works with no errors — validates the feature-flag decoupling early).

---

## Phase 3 — LitXus Retail Pro, Part 2: Inventory (3 weeks, ~15 days)

**Backend features:** Product CRUD, Warehouse CRUD, stock level tracking, stock movement recording (purchase/sale-issue/adjustment/transfer), FIFO/LIFO/Weighted-Average valuation engine, reorder-level alerts, inventory reports.

**Frontend components:** Product list/form, Warehouse setup, Stock level dashboard (below-reorder highlighted), Stock movement history per product, Valuation report, Reorder alert widget on dashboard.

**Database:** Inventory tables, §2.4.

**API:** 12+ endpoints, §3.7.

**Sample data:** 30–40 products, realistic stock quantities, 30–50 stock movements, valuation layers demonstrating FIFO vs weighted-average divergence for at least 3 products.

**Testing checklist:**
- [ ] Unit: FIFO layer consumption order correctness, weighted-average recalculation on each receipt, stock can't go negative on issue without override permission
- [ ] Integration: stock movement → stock level update consistency
- [ ] Manual: full cycle (receive stock → sell stock → verify valuation report matches expected FIFO cost)
- [ ] Edge cases: concurrent stock movements on same product (optimistic concurrency token), zero-quantity movement rejected
- [ ] Security: Inventory Manager can adjust stock, Sales User cannot

**Documentation:** Updated Swagger, user guide, Phase 3 completion report.

**Estimated effort:** ~15 days.

**Success criteria:** Inventory functions standalone alongside Sales (Retail Pro = Sales + Inventory, no Accounting needed) — validates the two-module combination.

---

## Phase 4 — LitXus Enterprise Pro: Integration Layer (2 weeks, ~10 days)

**Backend features:** `InvoicePostedEvent` → GL auto-posting handler (Dr AR / Cr Revenue / Cr SST Payable), `StockMovementRecordedEvent` → COGS GL posting handler, GL posting rules configuration (which accounts map to which transaction types), feature-flag toggle UI wired to real module enable/disable, license validation on startup.

**Frontend components:** Admin "Feature Flags" page (toggle Accounting/Sales/Inventory, disabled if not licensed), GL Posting Rules configuration screen, "test posting" dry-run preview.

**Database:** No new tables beyond what's already in place; `Licenses` table populated/validated.

**Testing checklist:**
- [ ] Integration: issuing an invoice with Accounting+Sales both enabled produces a correctly balanced GL entry
- [ ] Integration: same action with Accounting disabled produces no GL entry and no error
- [ ] Test all 4 module combinations: Accounting-only, Sales+Inventory (no Accounting), Accounting+Sales (no Inventory/no auto-post needed there), all three enabled
- [ ] Manual: toggle a module off mid-session, verify nav/API both reject subsequent access
- [ ] Edge case: GL posting rule misconfigured (missing account mapping) → clear error, not silent failure or unbalanced entry

**Documentation:** Integration OpenSpec, GL posting rule reference doc, Phase 4 completion report.

**Estimated effort:** ~10 days.

**Success criteria:** All four licensing combinations tested and pass; GL auto-postings always balance; disabling a module never crashes dependent screens (they simply don't render).

---

## Phase 5 — Polish, Testing & Release (2 weeks, ~10 days)

**Activities:** Performance pass (query N+1 audit, add missing indexes found under load, pagination defaults tuned), security audit (see [09_Testing_Strategy.md](09_Testing_Strategy.md) §Security), full regression across all 3 product configurations, complete documentation set for all products, deployment guide finalization for all 3 hosting options, Docker packaging, release notes, UAT sign-off.

**Deliverables checklist:**
- [ ] Load-tested with Phase 1–4 sample data volumes ×10
- [ ] Security audit report, all High/Critical findings resolved
- [ ] Complete user guide (all 3 products, screenshots)
- [ ] Deployment guide (self-hosted IIS, cloud-hosted, managed service)
- [ ] Docker Compose validated end-to-end (`docker compose up` → working app)
- [ ] Installation script / package
- [ ] Sample data SQL scripts finalized and versioned
- [ ] Complete OpenAPI spec exported and committed
- [ ] Architecture doc finalized
- [ ] UAT sign-off obtained
- [ ] `v1.0` tagged on `main`

**Estimated effort:** ~10 days.

## Total Effort Roll-Up

| Phase | Duration | Cumulative |
|---|---|---|
| 1 — Accounting Pro | 4 weeks | Week 4 |
| 2 — Retail Pro (Sales) | 3 weeks | Week 7 |
| 3 — Retail Pro (Inventory) | 3 weeks | Week 10 |
| 4 — Enterprise Pro (Integration) | 2 weeks | Week 12 |
| 5 — Polish & Release | 2 weeks | Week 14 |

~14 weeks core build, plus buffer → **4–5 months to v1.0**, matching the locked timeline.
