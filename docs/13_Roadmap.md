# 13 — Implementation Roadmap

## 13.1 Week-by-Week Timeline

| Week | Phase | Focus |
|---|---|---|
| 1 | 1 | OpenSpec (Accounting), Identity/RBAC/Audit foundation, CoA CRUD |
| 2 | 1 | GL entry CRUD + posting/voiding, tax code + SST calc |
| 3 | 1 | Bank reconciliation, reports (Trial Balance, Income Statement, Balance Sheet) |
| 4 | 1 | Testing, docs, Phase 1 sign-off, tag `v1.0-phase1` |
| 5 | 2 | OpenSpec (Sales), Customer CRUD, Invoice CRUD (Draft/Issue) |
| 6 | 2 | Payments + verification, Credit Notes, sequential numbering hardening |
| 7 | 2 | Sales reports, testing, docs, Phase 2 sign-off, tag `v1.0-phase2` |
| 8 | 3 | OpenSpec (Inventory), Product/Warehouse CRUD, stock levels |
| 9 | 3 | Stock movements, FIFO/LIFO/Weighted-Avg valuation engine |
| 10 | 3 | Inventory reports, testing, docs, Phase 3 sign-off, tag `v1.0-phase3` |
| 11 | 4 | OpenSpec (Integration), GL auto-posting events, posting rules config |
| 12 | 4 | Feature flag admin UI, combination testing, Phase 4 sign-off, tag `v1.0-phase4` |
| 13 | 5 | Performance pass, security audit |
| 14 | 5 | Full documentation set, deployment guide, Docker, UAT, release notes, tag `v1.0` |

(14 weeks core; add 2–6 weeks buffer for UAT feedback cycles and unforeseen issues → **4–5 months total**, matching the locked estimate.)

## 13.2 Milestones & Deliverables

| Milestone | Week | Deliverable |
|---|---|---|
| M1 — Accounting Pro sellable | 4 | Standalone Accounting product, demo-ready |
| M2 — Sales live | 7 | Retail Pro Part 1 usable standalone |
| M3 — Retail Pro complete | 10 | Sales + Inventory combined, sellable as Retail Pro |
| M4 — Enterprise Pro complete | 12 | All 3 modules + GL auto-posting, sellable as Enterprise Pro |
| M5 — v1.0 GA | 14 | All 3 products production-ready, fully documented |

## 13.3 Critical Path & Dependencies

```
Identity/RBAC/Audit (Phase 1 foundation)
        │
        ├──► Accounting module (Phase 1) ──► GL posting rules (Phase 4, needs Accounts to post into)
        │
        ├──► Sales module (Phase 2) ────────► InvoicePostedEvent (Phase 4, needs Invoice entity)
        │
        └──► Inventory module (Phase 3) ────► StockMovementRecordedEvent (Phase 4, needs StockMovement entity)

Phase 4 cannot start meaningfully until Phases 1–3 all have their core entities stable —
this is why Integration is Phase 4, not interleaved earlier. Phase 5 (polish/release)
depends on all four prior phases being feature-complete.
```

Everything downstream of the Identity/RBAC/Audit foundation (built in week 1) is why that work is front-loaded — a schema change to `Roles`/`Permissions` after Phase 2 starts would ripple through every subsequent controller.

## 13.4 Resource Requirements (by phase)

Assuming a small team (1–2 backend, 1 frontend, shared QA/PM effort — adjust to actual team size):

| Phase | Backend-heavy weeks | Frontend-heavy weeks | Shared testing/docs |
|---|---|---|---|
| 1 | 2.5 | 1.5 | 0.5 (overlapping) |
| 2 | 2 | 1 | throughout |
| 3 | 2 | 1 | throughout |
| 4 | 1.5 | 0.5 | throughout |
| 5 | — | — | 2 (dedicated) |

## 13.5 Risk Assessment & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Sequential numbering (invoices/GL entries) has gaps/collisions under concurrent load | Medium | High (compliance requirement) | Dedicated unit + integration tests simulating concurrent creates; DB-level sequence or serializable transaction, not app-level max()+1 |
| FIFO/LIFO valuation math has subtle bugs | Medium | High (financial accuracy) | Isolated, heavily-unit-tested valuation engine; hand-verified against manually-calculated seed data scenarios |
| Feature-flag decoupling leaks (Sales code accidentally hard-depends on Accounting) | Medium | Medium | Phase 2/3 explicitly tested with Accounting disabled as an acceptance criterion, not an afterthought |
| SST/MyInvois requirements change before launch (regulatory) | Low-Medium | Medium | Tax calculation isolated in a single service (`ISstCalculator`), not scattered — easy to update in one place |
| Scope creep within a phase delays downstream phases | Medium | Medium | OpenSpec's "Out of scope" section enforced at review; new asks go into a backlog for Phase 5+ or v1.1, not injected mid-phase |
| Single small team stretched across backend+frontend+QA | Medium | Medium | Testing checklist and OpenSpec review act as the quality gate regardless of team size — process compensates for headcount |

## 13.6 Go/No-Go Decision Points

At the end of each phase (§05, "Success criteria"): if a phase's sign-off checklist isn't met, the next phase does not start — either the current phase's scope is reduced (defer a nice-to-have) or the timeline slips for that phase only, rather than carrying known-broken foundations into the next module. Phase 4 in particular is a hard gate: integration work should not begin against unstable Phase 1–3 entities.
