## Context

LitXus Systems is a .NET 10 Clean Architecture backend (`backend/src/LitXus.Domain` → `LitXus.Application` → `LitXus.Infrastructure` → `LitXus.Api`) plus a React/TypeScript frontend (Konrix template, Redux Toolkit + Redux-Saga). It is a modular stand-alone product (not multi-tenant SaaS) — one codebase, with a license-driven feature-flag layer selecting which modules (Accounting / Sales / Inventory) a given deployment exposes. Five capabilities are already fully implemented: Identity/RBAC, Accounting, Company Profile, Licensing, Audit Trail. This design covers how those five map onto the codebase's layering, so specs stay traceable to real files.

## Goals / Non-Goals

**Goals:**
- Map each of the five baseline capabilities to its concrete Domain entities, Application handlers, Infrastructure services, API controllers, and frontend pages.
- Establish the convention that a capability's spec lives at `openspec/specs/<capability>/spec.md` and its `### Requirement:` sections should be traceable to an actual file/class, not invented.

**Non-Goals:**
- Not redesigning or refactoring any existing module.
- Not covering Sales/Inventory (schema exists in `docs/02_Database_Schema.md` §2.3–2.4 but no code is implemented yet — out of scope until those phases ship).
- Not migrating `docs/00-17` content into OpenSpec; those remain the canonical human-readable reference (see proposal's Impact section).

## Decisions

- **Layering convention per capability** — every capability crosses all four backend layers plus (usually) a frontend page:
  - `identity-auth` → Domain: `Modules/Identity/Entities` (Role, Permission, RolePermission, UserRole); Application: `Modules/Identity/*`; Infrastructure: `Identity/`, `Seeding/RbacSeeder.cs`; Api: `AuthController`, `AdminUsersController`, `AdminRolesController`; Frontend: `pages/admin/Users.tsx`, `pages/admin/Roles.tsx`, `pages/auth/*`.
  - `accounting` → Domain: `Modules/Accounting/Entities` (Account, GLEntry, GLEntryLine, TaxCode, BankAccount, BankStatementLine); Application: `Modules/Accounting/*`; Api: `AccountsController`, `GLEntriesController`, `AccountingReportsController`; Frontend: `pages/accounting/*`.
  - `company-profile` → Domain: `Modules/Shared/Entities/Company.cs`, `CompanySignatory.cs`; Application: `Modules/Company/*`; Api: `CompanyController`; Frontend: `pages/admin/CompanyProfile.tsx`, `components/ReportLetterhead.tsx`.
  - `licensing` → Domain: `Modules/Shared/Entities/License.cs`; Application: `Modules/Licensing/*`; Infrastructure: `Services/LicenseKeyVerifier.cs`, `Services/LicensingOptions.cs`, `Seeding/LicenseSeeder.cs`; offline tool: `backend/tools/LitXus.LicenseGenerator`; Api: `AdminLicenseController`; Frontend: `pages/admin/License.tsx`.
  - `audit-trail` → Domain: `Modules/Shared/Entities/AuditLog.cs`; Infrastructure: `Persistence/Interceptors/AuditSaveChangesInterceptor.cs`, `Services/AuditLogger.cs`; Api: `AdminAuditLogsController`; Frontend: `pages/admin/AuditLogs.tsx`.
- **One spec per capability, not one giant spec** — keeps future change proposals scoped to a single `specs/<capability>/spec.md` delta instead of a monolithic file.
- **This tool version (`@fission-ai/openspec@1.5.0`) has no `project.md`/`AGENTS.md`** — project context and the repo's documentation-update policy go in `openspec/config.yaml`'s `context:` field instead (confirmed by inspecting the actual scaffolded output and the installed `.claude/skills/*/SKILL.md` files, which reference `config.yaml` context, not a separate file).

## Risks / Trade-offs

- [Specs could drift from code as features evolve] → Mitigation: the repo's `CLAUDE.md` documentation-update policy (separate from this change) requires OpenSpec updates whenever business logic/workflow/architecture changes, checked before every implementation task.
- [Five specs written in one pass could contain inaccuracies since they're backfilled, not written test-first] → Mitigation: each spec's scenarios are drawn directly from reading the actual handler/verifier code (e.g. `LicenseKeyVerifier.cs`'s literal rejection strings), not paraphrased from memory, and spot-checked during verification.
