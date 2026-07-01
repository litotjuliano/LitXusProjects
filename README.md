# LitXus Systems

Modular stand-alone accounting/ERP software family for the Malaysia market — one monorepo, three products via feature flags:

- **LitXus Accounting Pro** — standalone GL, financial reports, tax, bank reconciliation
- **LitXus Retail Pro** — standalone Sales + Inventory
- **LitXus Enterprise Pro** — all modules with optional GL auto-posting integration

> Not to be confused with the earlier, unrelated `LitXusCount` project (abandoned learning project).

## Status

Planning complete. See [docs/00_Overview.md](docs/00_Overview.md) for the full blueprint (architecture, database schema, API spec, phase-by-phase roadmap, RBAC, audit trail, compliance, deployment). Phase 1 (Accounting Pro) implementation has not started — `backend/` and `frontend/` are scaffolded but empty.

## Structure

```
backend/    .NET 9 Clean Architecture solution (not yet initialized)
frontend/   React 18 + Vite app (not yet initialized)
docs/       Planning blueprint + per-phase OpenSpec documents
```

## Getting Started

See [docs/10_Deployment.md](docs/10_Deployment.md) §10.1 for local dev setup once Phase 1 scaffolding lands.
