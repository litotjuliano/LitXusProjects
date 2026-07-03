# 09 — Testing Strategy

## 9.0 Current Status (Phase 1)

- **`backend/tests/LitXus.UnitTests`** (49 tests, all passing): Domain-layer tests for the highest-risk
  business logic — `GLEntry` (balance validation, post/void lifecycle, line replacement), `Account`
  (debit/credit-normal balance math, trial balance column placement), `TaxCode` (SST rounding —
  half-away-from-zero, not banker's rounding, per [15_Malaysia_Compliance.md](15_Malaysia_Compliance.md)
  §15.1), `License`, `BankStatementLine`. Plus Application-layer handler tests for the GL entry commands
  (Create/Post/Void/Update) against a real `AppDbContext` backed by EF Core's **InMemory** provider —
  this exercises real change-tracking behavior, which is how the `UpdateGLEntryCommandHandler`
  Added-vs-Modified regression (client-generated GUID PKs misidentified as `Modified`) is now guarded
  against by a regression test. Also covers `RequirePermissionAttribute` (403 without the claim) and
  `LicenseKeyVerifier` (accepts a token signed by the matching key, rejects a wrong-keypair signature
  and an expired token — the exact failure mode that broke license activation earlier in development).
- **`backend/tests/LitXus.IntegrationTests`** (8 tests, all passing): real HTTP round-trip tests via
  `WebApplicationFactory<Program>` against the **local SQL Server instance** (not Testcontainers — no
  Docker daemon is available in this dev environment; see §9.3 for why and what that trades off).
  Covers 401 without a token, 403 for a Viewer (read-only role) attempting a mutating endpoint, the
  full Account lifecycle (create → update → deactivate → reactivate) and the full GL Entry lifecycle
  (create Draft → Post → Void, plus 422 on an unbalanced entry) — all through real controllers, real
  MediatR handlers, real EF Core migrations, and real RBAC/demo seeding, not mocked.
- No formal line-coverage measurement has been run yet — the >80% target in §9.1 is aspirational, not
  currently measured or enforced.

## 9.1 Unit Testing (.NET)

- **Framework:** xUnit + Moq + FluentAssertions.
- **Scope:** Domain entity business rules (e.g. `GLEntry.Post()` throws if unbalanced), Application command/query handlers (repositories mocked), validators (FluentValidation `TestValidate`), the valuation engine (FIFO/LIFO/WeightedAverage math — the highest-risk logic in the whole system, gets the deepest test coverage).
- **Target:** >80% line coverage on Domain + Application layers (Infrastructure/Api layers covered mainly by integration tests instead, since they're thin wiring).
- **Location:** `backend/tests/LitXus.UnitTests`, mirroring `Modules/{Module}/...` folder structure of the code under test.

## 9.2 Unit Testing (React)

- **Framework:** Vitest + React Testing Library.
- **Scope:** Form validation logic, currency/date formatting utils, Zustand store actions (e.g. does `authStore.logout()` clear all fields), permission-gated rendering (`usePermission` hook hides/shows correctly).

## 9.3 Integration Testing

- **Framework:** xUnit + `WebApplicationFactory<Program>` + a real SQL Server instance — not mocked, to catch EF Core query/migration issues mocks would hide.
- **Database:** as originally designed, this should run against a disposable **Testcontainers** SQL
  Server instance so CI needs nothing pre-installed. In this dev environment Docker isn't available, so
  `ApiWebApplicationFactory` instead points at the **same local SQL Server instance `scripts/run-dev.bat`
  already uses**, creating a uniquely-named throwaway database (`LitXusSystems_IntegrationTest_<guid>`)
  per test run and dropping it on teardown (`IAsyncLifetime.DisposeAsync`) — verified via `sqlcmd` that no
  test database is left behind after a run. This is a real trade-off, not a strict downgrade: it exercises
  actual migrations and actual RBAC/demo seeding on every run (Testcontainers tests typically seed a
  minimal fixed dataset instead, per §9.7), at the cost of requiring a local SQL Server instance rather
  than working out of the box on any machine with Docker. **If/when Docker becomes available** (e.g. a
  CI runner), swapping `ApiWebApplicationFactory`'s connection-string source back to a `Testcontainers.MsSql`
  container is a self-contained change — no test class needs to change, only the fixture.
- **Scope:** Full HTTP round-trip per endpoint: request → controller → MediatR → handler → DB → response. Currently covers auth (401 without/with a malformed token), permissions (403 for a Viewer attempting a mutating endpoint), and the full CRUD/lifecycle round-trip for Accounts and GL Entries (including the 422 unbalanced-entry business-rule case). Module-gating (403 with a module disabled) and the rest of the CRUD/lifecycle matrix across other entities remain outstanding — not yet written.
- **Location:** `backend/tests/LitXus.IntegrationTests/Accounting`, one test class per concern (`AuthenticationTests`, `PermissionTests`, `AccountLifecycleTests`, `GLEntryLifecycleTests`).

## 9.4 Test Scenarios — By Category (applied per module, per phase)

**Happy path:**
- Create → Read → Update → soft-Delete/deactivate, for every entity
- Full business lifecycle per entity (Draft→Posted GL entry, Draft→Issued→Paid invoice, Purchase→Sale stock movement)
- Report generation against known seed data produces mathematically correct output (trial balance sums to zero, invoice totals match line sums, stock valuation matches expected FIFO order)

**Error cases:**
- Missing required fields → 400 with field-level messages
- Invalid foreign keys (e.g. non-existent CustomerId) → 400 or 404
- Business rule violations → 422 (unbalanced GL entry, voiding a paid invoice, negative stock without override)
- Duplicate unique fields (account code, invoice number collision under race condition) → 409

**Edge cases:**
- Boundary values: zero-amount invoice, single-line GL entry, exactly-at-reorder-level stock
- Large data: pagination correctness at page boundaries, sorting stability, export of 1,000+ rows doesn't time out
- Concurrency: two users editing the same stock level simultaneously (optimistic concurrency token / row version)
- Date edge cases: invoice due date before invoice date rejected, GL entry dated in a closed period rejected (future-phase, documented as known limitation if not in v1.0)

**Security testing:**
- Every mutating endpoint tested with a user lacking the required permission → 403
- Every module-gated endpoint tested with that module disabled → 403
- JWT tampering (expired token, malformed token, token for deactivated user) → 401
- SQL injection / XSS payloads in text fields → rejected or safely encoded (EF Core parameterization + React's default JSX escaping cover most of this; explicitly tested, not just assumed)
- IDOR check: user A cannot fetch/edit user B's data via guessed IDs where scoping should apply (mostly N/A here since this is single-tenant per install, but still checked for e.g. non-admins listing all users)

## 9.5 Performance Testing (Phase 5)

- Load test with ×10 sample data volume ([08_Sample_Data.md](08_Sample_Data.md) §8.6) using k6 or similar against report endpoints (trial balance, valuation, AR aging — the query-heaviest operations).
- Target: report endpoints <2s at ×10 volume; list endpoints <500ms with pagination.
- N+1 query audit via EF Core logging (`Microsoft.EntityFrameworkCore` log category set to `Information` in a load-test run, scanned for repeated per-row queries).

## 9.6 UAT (User Acceptance Testing)

- Conducted at the end of each phase with real end-user personas (an actual accountant reviews Phase 1, a retailer reviews Phase 2/3) walking through the OpenSpec `Test_Scenarios.md` happy-path list live.
- Sign-off is a named checklist item in each phase's completion report ([05_Phase_Breakdown.md](05_Phase_Breakdown.md)) — a phase is not "done" without it.

## 9.7 Test Data Seeding Strategy for Tests

- Unit tests: in-memory fakes/builders (e.g. `GLEntryBuilder.WithLines(...).Build()`), no database at all.
- Integration tests: fresh database per test run (see §9.3 for the current local-SQL-Server vs. Testcontainers trade-off), migrated and seeded with the **full** RBAC + demo-account + demo-data seed (not a minimal fixed dataset) since `Seeding:Enabled` isn't overridden — this exercises the real seeding pipeline as a side effect, but means tests must not assert on exact report totals (which drift as `AccountingDemoDataSeeder`'s data evolves) — only on entities the test itself creates.

## 9.8 CI Gate

`.github/workflows/ci.yml` runs on every PR into `develop`/`main`: restore → build → unit tests → integration tests → frontend lint/typecheck → frontend unit tests → (Phase 5+) Docker image build smoke test. A PR cannot merge with a red build. `.github/workflows/ci.yml` doesn't exist yet in this repo (no CI has been set up) — if/when it is, it needs a SQL Server service (Testcontainers if the runner has Docker, else a `mssql` service container) for `LitXus.IntegrationTests` to run against; see §9.3.
