## 1. Backend

- [x] 1.1 Add `CreateUserAsync` to `IIdentityUserService` and implement in `IdentityUserService`
- [x] 1.2 Add `CreateUserCommand`/`CreateUserCommandHandler`/`CreateUserValidator`
- [x] 1.3 Add `POST /api/v1/admin/users` to `AdminUsersController`
- [x] 1.4 Restrict `IdentityService.RegisterAsync` to bootstrap-only
- [x] 1.5 Seed one demo account per role (all 7) in `UserSeeder`

## 2. Frontend

- [x] 2.1 Add `createUser()` to `helpers/api/admin.ts`
- [x] 2.2 Add "+ New User" modal to `Users.tsx`
- [x] 2.3 Add per-row role dropdown to `Users.tsx` (wired to existing `assignRole`/`revokeRole`)
- [x] 2.4 Update `Register.tsx` copy for its bootstrap-only purpose
- [x] 2.5 Add the 5 new seeded accounts to `Login.tsx`'s dev-only demo panel

## 3. Documentation

- [x] 3.1 CLAUDE.md + openspec/config.yaml — Post-Task Documentation Checklist
- [x] 3.2 OpenSpec identity-auth spec (this change)
- [x] 3.3 docs/03_API_Specification.md — new endpoint + changed register behavior
- [x] 3.4 Admin_Setup_User_Guide.md — rewrite §3, update §1 table, Not Yet Built list, new screenshots
- [x] 3.5 Business_Rules.md, Features.md — update registration rule
- [x] 3.6 docs/10_Deployment.md §10.3a — onboarding step now UI-driven

## 4. Verification

- [x] 4.1 Backend + frontend build clean
- [x] 4.2 Drop/reseed dev DB, confirm all 7 seeded accounts + Login page demo panel
- [x] 4.3 End-to-end: create user via UI, change role via UI, confirm audit log entries
- [x] 4.4 Confirm Super Admin not offered as an option anywhere in the new UI
- [x] 4.5 Confirm `/auth/register` rejects a second call against the dev DB
- [x] 4.6 Re-run fresh-production-install simulation: first register succeeds (Super Admin), second is rejected
