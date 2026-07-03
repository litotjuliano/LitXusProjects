# identity-auth Specification

## Purpose
TBD - created by archiving change baseline-existing-system. Update Purpose after archive.
## Requirements
### Requirement: Super Admin cannot be assigned or revoked through the general role endpoints
`AssignRoleCommandHandler` and `RevokeRoleCommandHandler` SHALL reject any attempt to assign or revoke the "Super Admin" role, regardless of the caller's own permissions — Super Admin is provisioned only via seeding.

#### Scenario: An Admin cannot self-escalate to Super Admin
- **WHEN** a user holding `Admin.Users.Update` calls the assign-role endpoint with the Super Admin role ID (their own or another user's)
- **THEN** the system rejects it with `ForbiddenException("The Super Admin role cannot be assigned through this endpoint.")`

#### Scenario: Super Admin's role cannot be revoked via the API
- **WHEN** a revoke-role request targets the Super Admin role
- **THEN** the system rejects it with `ForbiddenException("The Super Admin role cannot be revoked through this endpoint.")`

### Requirement: Super Admin is excluded from general user and role listings
`GetRolesQueryHandler` and `IdentityUserService.GetUsersAsync` SHALL exclude the Super Admin role and any user holding it, respectively, from their results — Super Admin is the install owner and must not be inspectable or manageable through the general admin UI.

#### Scenario: Super Admin does not appear in the Users list
- **WHEN** an Admin views Administration → Users
- **THEN** the account holding the Super Admin role is not present in the list, even though it exists in the database

#### Scenario: Super Admin does not appear in the Roles & Permissions list
- **WHEN** an Admin views Administration → Roles & Permissions
- **THEN** the "Super Admin" role is not present in the list

### Requirement: Access tokens are short-lived HMAC-SHA256 JWTs with a separate refresh token
The system SHALL issue access tokens signed with `HmacSha256`, embedding `sub` (user ID), `email`, `name`, one `role` claim per assigned role, one `permission` claim per distinct permission code, and one `module` claim per enabled module. Access tokens SHALL default to a 30-minute lifetime (`Jwt:AccessTokenMinutes`); a separate refresh token (64 random bytes, Base64-encoded, stored in ASP.NET Identity's user-tokens table) SHALL default to a 14-day lifetime (`Jwt:RefreshTokenDays`).

#### Scenario: Expired access token is rejected but refresh token remains valid
- **WHEN** an access token issued 31 minutes ago (with default config) is presented to a protected endpoint
- **THEN** the request is rejected as unauthenticated, but the paired refresh token (issued within the last 14 days) can still be exchanged for a new access token

### Requirement: Login rejects unknown emails, inactive accounts, and wrong passwords with the same generic message for credential failures
`IdentityService.LoginAsync` SHALL check, in order: the email exists, the account is active, and the password matches — using the same generic `"Invalid credentials."` message for both unknown-email and wrong-password cases (to avoid user enumeration), and a distinct `"This account is not active."` message for deactivated accounts.

#### Scenario: Wrong password and unknown email produce identical error text
- **WHEN** login is attempted with an email that doesn't exist, versus a valid email with the wrong password
- **THEN** both cases return the same `"Invalid credentials."` message, not distinguishable by the caller

#### Scenario: Successful login updates last-login timestamp
- **WHEN** a login succeeds
- **THEN** the user's `LastLoginAtUtc` is set to the current time and both access and refresh tokens are issued

### Requirement: Permission checks are claim-based, not DB round-trips per request
`[RequirePermission("Code")]` SHALL check the caller's JWT `permission` claims directly (no database lookup per request) and SHALL reject with a generic 403 `"You do not have permission to perform this action."` if absent, to avoid revealing which specific permissions exist.

#### Scenario: Missing permission yields a generic 403, not a specific list of what's required
- **WHEN** a user without `Admin.License.Update` calls the apply-license-key endpoint
- **THEN** the response is 403 with the generic message, not an enumeration of the required permission

### Requirement: Module gating is enforced independently of permission checks
`[RequireModule("ModuleName")]` SHALL call `IFeatureFlagService.IsEnabled(module)` and reject with 403 and error code `MODULE_NOT_ENABLED` if the module isn't licensed/enabled for the installation — independent of whether the caller otherwise has permission to use it.

#### Scenario: A permitted user is still blocked if the module is disabled
- **WHEN** a user with full Accounting permissions calls an Accounting endpoint on a deployment whose license doesn't include the Accounting module
- **THEN** the request is rejected with `MODULE_NOT_ENABLED: "The Accounting module is not enabled for this installation."`

### Requirement: Permission codes follow a fixed Module.Entity.Operation naming convention
`PermissionCatalog` SHALL define every permission as `{Module}.{Entity}.{Operation}` (e.g. `Admin.License.Update`, `Company.Profile.Read`), generated in code so seeded permissions can never drift from the `[RequirePermission]` attributes that reference them.

#### Scenario: A new permission is added
- **WHEN** a new endpoint needs a new permission
- **THEN** it is added to `PermissionCatalog` in the `Module.Entity.Operation` pattern and the corresponding `[RequirePermission]` attribute references the same code string, keeping seed data and enforcement in sync by construction

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

### Requirement: Admin/Super Admin can reset a user's password directly, except the Super Admin account
`POST /api/v1/admin/users/{id}/reset-password` (requiring `Admin.Users.Update`) SHALL set a new password for an existing user in one atomic, server-side operation — no reset token is ever returned to the caller or any anonymous party. It SHALL reject resetting the Super Admin account's password, using the same protection as role assignment and user creation.

#### Scenario: Admin resets a locked-out Accountant's password
- **WHEN** an Admin submits `POST /admin/users/{id}/reset-password` for a user with the Accountant role
- **THEN** the password is changed immediately, and the user can log in with the new password right away

#### Scenario: Resetting the Super Admin's password is rejected
- **WHEN** the target user holds the "Super Admin" role
- **THEN** the request is rejected with `ForbiddenException("The Super Admin account's password cannot be reset through this endpoint.")`

### Requirement: There is no self-service password reset
`POST /auth/forgot-password` and any anonymous password-reset request flow SHALL NOT exist — no email/SMTP infrastructure exists to deliver a reset token safely, and returning a reset token to an unauthenticated caller would allow account takeover by anyone who knows a user's email address.

#### Scenario: A locked-out user is directed to contact an Administrator
- **WHEN** a user visits the password-recovery page
- **THEN** they see instructions to contact their Administrator, not a form that submits to a live reset endpoint

