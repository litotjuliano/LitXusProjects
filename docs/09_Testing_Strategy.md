# 09 — Testing Strategy

## 9.1 Unit Testing (.NET)

- **Framework:** xUnit + Moq + FluentAssertions.
- **Scope:** Domain entity business rules (e.g. `GLEntry.Post()` throws if unbalanced), Application command/query handlers (repositories mocked), validators (FluentValidation `TestValidate`), the valuation engine (FIFO/LIFO/WeightedAverage math — the highest-risk logic in the whole system, gets the deepest test coverage).
- **Target:** >80% line coverage on Domain + Application layers (Infrastructure/Api layers covered mainly by integration tests instead, since they're thin wiring).
- **Location:** `backend/tests/LitXus.UnitTests`, mirroring `Modules/{Module}/...` folder structure of the code under test.

## 9.2 Unit Testing (React)

- **Framework:** Vitest + React Testing Library.
- **Scope:** Form validation logic, currency/date formatting utils, Zustand store actions (e.g. does `authStore.logout()` clear all fields), permission-gated rendering (`usePermission` hook hides/shows correctly).

## 9.3 Integration Testing

- **Framework:** xUnit + `WebApplicationFactory<Program>` + a real (containerized, via Testcontainers) SQL Server instance — not mocked, to catch EF Core query/migration issues mocks would hide.
- **Scope:** Full HTTP round-trip per endpoint: request → controller → MediatR → handler → DB → response. Covers auth (401 without token, 403 without permission, 403 without licensed module), validation (400 on bad input), and the full CRUD lifecycle per entity.
- **Location:** `backend/tests/LitXus.IntegrationTests`, one test class per controller.

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
- Integration tests: fresh Testcontainers SQL Server instance per test collection, migrated + seeded with a minimal fixed dataset (not the full demo seed — a small, deterministic set so assertions on report totals are exact and don't drift as demo data evolves).

## 9.8 CI Gate

`.github/workflows/ci.yml` runs on every PR into `develop`/`main`: restore → build → unit tests → integration tests (Testcontainers) → frontend lint/typecheck → frontend unit tests → (Phase 5+) Docker image build smoke test. A PR cannot merge with a red build.
