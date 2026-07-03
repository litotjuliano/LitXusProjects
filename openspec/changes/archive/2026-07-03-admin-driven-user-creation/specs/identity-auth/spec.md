## ADDED Requirements

### Requirement: Self-registration only succeeds for the very first user on an install
`IdentityService.RegisterAsync` SHALL only create an account when no user exists yet (`!userManager.Users.AnyAsync()`) — that one account is auto-activated and granted Super Admin. Every subsequent call SHALL be rejected, regardless of email/password validity.

#### Scenario: First registration on a fresh install succeeds and grants Super Admin
- **WHEN** `POST /auth/register` is called and no user exists in the system yet
- **THEN** the account is created `IsActive: true`, granted the Super Admin role, and can immediately use Super-Admin-only endpoints (e.g. `Admin.License.Update`)

#### Scenario: A second registration attempt is rejected
- **WHEN** `POST /auth/register` is called after at least one user already exists
- **THEN** the request is rejected with `AuthenticationException("Self-registration is disabled. Ask an administrator to create your account.")`, and no account is created

### Requirement: Admin/Super Admin can create a fully-usable user account in one step
`POST /api/v1/admin/users` (requiring `Admin.Users.Update`) SHALL create a new user — active immediately, no Pending state — with the given email, full name, password, and role, all in one atomic operation. It SHALL reject granting the Super Admin role through this endpoint, using the same protection as role assignment.

#### Scenario: Admin creates a new Accountant account
- **WHEN** an Admin submits `POST /api/v1/admin/users` with a role ID resolving to "Accountant"
- **THEN** the account is created `IsActive: true` with the Accountant role already assigned, and the new user can log in and use Accountant-permission endpoints immediately — no separate activation step

#### Scenario: Creating a user with the Super Admin role is rejected
- **WHEN** the submitted `roleId` resolves to the "Super Admin" role
- **THEN** the request is rejected with `ForbiddenException("The Super Admin role cannot be granted through this endpoint.")`, matching `AssignRoleCommandHandler`'s existing guard
