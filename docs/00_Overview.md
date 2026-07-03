# LitXus Systems — v1.0 Planning Blueprint

**Company:** LitXus Systems
**Products:** LitXus Accounting Pro · LitXus Retail Pro · LitXus Enterprise Pro
**Architecture:** Modular stand-alone (NOT multi-tenant SaaS) — one monorepo, one codebase, feature flags select which modules a given installation exposes.
**Market:** Malaysia (SMEs, accountants/CPAs/bookkeepers, retailers, mid-size distributors)
**Method:** Spec-driven development — OpenSpec docs are written and reviewed before any code for a phase is implemented.

> This supersedes and is unrelated to "LitXusCount" (the earlier multi-tenant SaaS learning project). Do not port assumptions, schema, or code from LitXusCount into this project.

> **Two "OpenSpec" layers coexist by design, per capability vs. per phase:** this `docs/` tree (including the per-phase `docs/11_OpenSpec_Template.md` template and `docs/phase-N-*/` folders) is the human-readable planning reference. `openspec/specs/` (installed via the real [OpenSpec CLI](https://github.com/Fission-AI/OpenSpec), `@fission-ai/openspec`) is a separate, machine-readable layer organized by capability (`identity-auth`, `accounting`, `company-profile`, `licensing`, `audit-trail`) that AI coding assistants read directly before implementing changes — see root `CLAUDE.md` for the full documentation-update policy. The REST API contract itself is documented separately via OpenAPI/Swagger (§3, `docs/03_API_Specification.md`) — OpenSpec does not duplicate it.

## Product-to-Module Matrix

| Product | Accounting | Sales | Inventory | GL Auto-Posting | Price (MYR/yr) |
|---|---|---|---|---|---|
| LitXus Accounting Pro | ✅ | ❌ | ❌ | N/A | 2,000–3,000 |
| LitXus Retail Pro | ❌ | ✅ | ✅ | N/A | 1,500–2,500 |
| LitXus Enterprise Pro | ✅ | ✅ | ✅ | ✅ optional | 4,000–5,000 |

A single license row (`Licenses` table, see [02_Database_Schema.md](02_Database_Schema.md)) determines which modules a deployed instance may use. The frontend and backend both read this flag set at runtime — there is no per-product build.

## Document Index

| # | Document | Covers |
|---|---|---|
| 00 | Overview | This file |
| 01 | [Architecture](01_Architecture.md) | System architecture, Clean Architecture layers, module boundaries, data flow |
| 02 | [Database Schema](02_Database_Schema.md) | Full schema, ER relationships, indexes, constraints |
| 03 | [API Specification](03_API_Specification.md) | Endpoint inventory, request/response shapes, error contract |
| 04 | [Project Structure](04_Project_Structure.md) | Backend/frontend folder layout, naming conventions, git strategy |
| 05 | [Phase Breakdown](05_Phase_Breakdown.md) | Deliverables, checklists, effort estimate per phase |
| 06 | [RBAC & Auth](06_RBAC_Auth.md) | Roles/permissions model, JWT lifecycle, guard implementation |
| 07 | [Audit Trail](07_Audit_Trail.md) | What's audited, table design, interceptor pattern, retention |
| 08 | [Sample Data Strategy](08_Sample_Data.md) | Seed data volumes, Malaysia context, seeding mechanism |
| 09 | [Testing Strategy](09_Testing_Strategy.md) | Unit/integration/UAT approach per module |
| 10 | [Deployment Architecture](10_Deployment.md) | Dev/demo/production options, Docker, backup/recovery |
| 11 | [OpenSpec Template](11_OpenSpec_Template.md) | Reusable per-phase spec template + one worked example |
| 12 | [Documentation Templates](12_Documentation_Templates.md) | User/dev/deployment guide skeletons |
| 13 | [Implementation Roadmap](13_Roadmap.md) | Week-by-week timeline, critical path, risk register |
| 14 | [Technology Implementation](14_Tech_Implementation.md) | .NET 10 + React setup specifics, MediatR, EF Core, JWT, Swagger |
| 15 | [Malaysia Compliance](15_Malaysia_Compliance.md) | SST, MyInvois, PDPA, Companies Act 2016 |
| 16 | [Feature Flags & Packaging](16_Feature_Flags.md) | Flag mechanism, licensing logic, product config examples |
| 17 | [License Generator](17_License_Generator.md) | Offline signing tool (`backend/tools/LitXus.LicenseGenerator`) — keypair/license generation, applying keys, rotation, troubleshooting |
| — | [Phase 1 User Guide](phase-1-accounting/User_Guide.md) | Click-by-click walkthrough for manually keying in Chart of Accounts / GL Entries / Reports through the actual UI, with screenshots |
| — | [Phase 1 Admin & System Setup Guide](phase-1-accounting/Admin_Setup_User_Guide.md) | Hands-on walkthrough of License management (Super Admin), onboarding a new user and assigning a role (Admin), Company Profile setup, and what a restricted User-tier account actually sees, with screenshots |

## Locked Technology Stack

**Backend:** .NET 10 (LTS — upgraded from the originally-locked .NET 9 once .NET 10 shipped as LTS in Nov 2025; see [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.1), C# 14, Clean Architecture (Presentation / Application / Domain / Infrastructure), EF Core 10, SQL Server 2019+, ASP.NET Core Identity + JWT, Serilog, MediatR (pinned to the last pre-commercial-license release), FluentValidation, xUnit + Moq/FluentAssertions, Swagger/OpenAPI 3.0. **No AutoMapper** — dropped after v13+ requires a commercial license and the free v12.x line has an unpatched DoS vulnerability (CVE-2026-32933); Entity↔DTO mapping is hand-written via extension methods instead (see [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.2a).

**Frontend:** [Konrix](https://coderthemes.com) Envato Tailwind admin template (Vite + React 18 + TypeScript, used as-is per the locked spec) — Redux Toolkit + Redux-Saga for state (the template's own architecture, not Zustand), Tailwind CSS v3 + FrostUI + Headless UI, React Router v6, React Hook Form + Yup, Axios + jwt-decode, Day.js, ApexCharts (the template's charting library, not Recharts), ESLint + Prettier, ships without a test runner (Vitest to be added in Phase 5 if unit-testing the custom LitXus pages becomes a priority). See [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.5 for how the template was integrated and where it diverges from the original React-18-generic stack description.

**Repo:** GitHub monorepo — `/backend`, `/frontend`, `/docs`. Branches: `main` (prod), `develop` (integration), `feature/*`.
