## Context

Two architecture questions were resolved with the user before implementation:
1. How does Sales post to the GL without Sales depending on Accounting at compile time?
2. How does Sales know which GL accounts to post to?

Both were confirmed via explicit user choice rather than assumed.

## Goals / Non-Goals

**Goals:**
- Implement the full `docs/02_Database_Schema.md` §2.3 / `docs/03_API_Specification.md` §3.6 Sales contract.
- Make Sales work fully standalone when Accounting isn't licensed (Phase 2's own stated success criterion).
- Reuse the domain-event mechanism for Phase 3's Inventory→COGS posting later, rather than building a one-off integration.

**Non-Goals:**
- `ProductId` on `InvoiceLine` — the `Products` table doesn't exist until Phase 3; every Phase 2 line is free-text (`Description`/`Quantity`/`UnitPrice`), plus a lightweight free-text `UnitOfMeasure` string added after a user follow-up question. The user separately specified a full multi-level UOM conversion engine (Carton→Box→Pack hierarchies, per-item conversion factors, a UOM Master + Item UOM Conversion table) — that is explicitly Phase 3 (Inventory) scope, not built here, and is captured for that phase in project memory.
- GL posting for Credit Notes — the 15-endpoint spec only exposes create+read for credit notes; posting is flagged as a gap for a future change if it comes up in practice.
- Recharts for the 2 Sales reports — `docs/05_Phase_Breakdown.md` suggested Recharts, but the existing Accounting reports (`TrialBalance.tsx`, `GeneralLedger.tsx`, etc.) are all plain tables and Recharts isn't currently a dependency; Sales Summary/AR Aging follow the same plain-table convention rather than introducing a new frontend dependency for this one pass.

## Decisions

- **GL integration: real domain-event pattern, not a direct service call.** `BaseEntity` queues `IDomainEvent`s; a new `DomainEventDispatchInterceptor` (mirrors the existing `AuditSaveChangesInterceptor`) dispatches them via MediatR *after* `SaveChangesAsync` commits (not before — a failed transaction must never fire an event). Each event is wrapped in `DomainEventNotification<TDomainEvent> : INotification` via reflection (`Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(...), domainEvent)`), since `IDomainEvent` lives in Domain and must never reference MediatR. `PostInvoiceToGLHandler`/`PostPaymentToGLHandler` check `IFeatureFlagService.IsEnabled(Module.Accounting)` and no-op otherwise — chosen over conditional DI registration because MediatR's `RegisterServicesFromAssembly` auto-discovery makes "not registered when unlicensed" awkward to express, and a licensed-check no-op is behaviorally identical.
- **`SalesSettings` singleton entity**, not per-invoice account selection — matches the existing `Company`/`License` singleton-row convention. `IsConfigured` requires all 4 accounts set; `SalesSettingsNotConfiguredException` is thrown by the posting handlers (not by `Invoice.Issue()` itself, since Issue must succeed standalone without Accounting).
- **Invoice/Payment lifecycle split**: recording a payment (`Payment.Create`, `Status = Pending`) never touches the invoice balance — only `Payment.Verify()` (which raises `PaymentVerifiedEvent`) applies the amount via `Invoice.ApplyPayment()`, and the handler applies the invoice-side effect *before* flipping the payment to `Verified`, so a failed apply never leaves a falsely-verified payment. This matches the existing `MatchBankStatementLineCommandHandler` precedent of handler-orchestrated cross-entity invariants rather than entities reaching into each other.
- **`CreditNote` collapses Issue+Apply into one `Create` step** — the API spec only exposes create+read, and a credit note is already scoped to exactly one invoice at creation, so there's no intermediate state worth modeling.
- **`Overdue` is computed at query time**, never a stored/background-job-driven transition — no scheduled-job infrastructure exists in this codebase, and `DueDate < today && Status is Issued or PartiallyPaid` is always correct without one.
- **RbacSeeder made additive.** Discovered via live testing: `Sales.Settings.Update` returned 403 for Super Admin even though the permission and grant were coded, because the original seeder was `if (await db.Permissions.AnyAsync()) return;` — a one-shot guard that skipped entirely once Phase 1's permissions existed, so Phase 2's new `Sales.*` permissions and role grants never reached the already-seeded dev database. Fixed to fetch-or-create permissions by `Code` and roles by `Name`, then re-run every role's grant list every startup (safe because `Role.GrantPermission` already no-ops if already granted). This is now the pattern Phase 3's Inventory permissions will rely on too — otherwise every future phase hits the same silent gap on any database that already has Phase 1 seeded.

## Risks / Trade-offs

- [JWT-embedded permission claims mean a user must re-login after a role's grants change] → Pre-existing architecture decision from Phase 1 (`docs/06_RBAC_Auth.md` §6.3), not introduced here; noted because the RbacSeeder fix only takes effect for a role's *next* login, not an already-active session.
- [No GL posting for Credit Notes] → A real gap if a business wants credit notes reflected in the ledger; out of scope for this pass, flagged for a future change.
- [Free-text `UnitOfMeasure` instead of a UOM master/conversion engine] → Deliberately deferred; Phase 3 Inventory will need the full conversion engine the user specified, and this Phase 2 field will very likely be replaced rather than extended.
