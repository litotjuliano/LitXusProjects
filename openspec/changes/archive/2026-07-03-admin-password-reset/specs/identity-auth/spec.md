## ADDED Requirements

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
