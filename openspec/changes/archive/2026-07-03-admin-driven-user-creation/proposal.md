## Why

Self-registration (`POST /auth/register`) was the only way to onboard a user, but it created accounts unroled and Pending, requiring a follow-up API-only role assignment with no UI. This left onboarding practically broken (this session had to use `curl` to onboard a demo account) and meant anyone reachable could self-register at all, even though the intended model is that only Super Admin/Admin create accounts.

## What Changes

- `POST /auth/register` now only succeeds for the very first user ever (fresh-install bootstrap, granted Super Admin) — every subsequent call is rejected with a clear error instead of creating a Pending, unroled account.
- New `POST /api/v1/admin/users` lets an Admin/Super Admin create a user (name, email, password, role) in one step, active immediately.
- New UI: a "+ New User" modal and a per-row role dropdown on the Users page, wired to the (previously unused) `assignRole`/`revokeRole` endpoints.
- Dev/demo seeding now creates one account per role (all 7), not just Super Admin/Admin.

## Capabilities

### Modified Capabilities
- `identity-auth`: self-registration is now bootstrap-only; adds an Admin-driven user-creation requirement and its own Super Admin exclusion guard (matching the existing role-assignment guard).

### New Capabilities
None.

## Impact

- Backend: new `CreateUserCommand`/Handler/Validator, new `IIdentityUserService.CreateUserAsync`, `IdentityService.RegisterAsync` behavior change, new `AdminUsersController` endpoint.
- Frontend: `Users.tsx` (new modal + role dropdown), `admin.ts` (`createUser`), `Register.tsx` (copy only), `Login.tsx` (demo accounts list).
- Docs: `docs/03_API_Specification.md` (new endpoint + changed register behavior), `docs/phase-1-accounting/Admin_Setup_User_Guide.md`, `Business_Rules.md`, `Features.md`, `docs/10_Deployment.md` §10.3a.
- No database schema changes.
