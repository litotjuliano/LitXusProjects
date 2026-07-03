## Why

`Features.md` listed "Password reset flow (request + confirm) works end-to-end" as unbuilt, and the frontend already had a "forgot password" request page pointing at a backend endpoint (`/auth/forgot-password`) that didn't exist — a genuinely broken UX path, not just a missing feature. Building a self-service flow would mean deciding how a reset token reaches an anonymous caller with no email/SMTP infrastructure in this project; returning the token directly in the response would let anyone who knows a user's email reset that user's password with zero authentication — a real account-takeover hole. User confirmed: skip self-service, build Admin-initiated reset instead, matching the trust model already used for account creation.

## What Changes

- New `POST /api/v1/admin/users/{id}/reset-password { newPassword }` — an authenticated Admin/Super Admin sets a new password for an existing user directly (generates and consumes an Identity password-reset token server-side in one call; no token ever leaves the server).
- Rejects resetting the Super Admin account's password through this endpoint (same protection already used for role assignment and user creation).
- New "Reset Password" button + modal on the Users admin page.
- `RecoverPassword.tsx` (the broken "forgot password" request page) rewritten from a form calling a nonexistent endpoint into an informational page directing the user to contact their Administrator — removes the broken UX path rather than leaving it half-wired.

## Capabilities

### Modified Capabilities
- `identity-auth`: adds Admin-initiated password reset, with the same Super Admin exclusion already established for role assignment/user creation.

## Impact

- Backend: `IIdentityUserService.ResetUserPasswordAsync`, `ResetUserPasswordCommand`/Handler/Validator, new endpoint on `AdminUsersController`.
- Frontend: `resetUserPassword()` helper, Reset Password modal on `Users.tsx`, `RecoverPassword.tsx` rewritten (no longer dispatches the dead `FORGOT_PASSWORD` redux action, though that action/saga/reducer scaffolding itself is left in place as inert unused code rather than a wider redux refactor).
- No database schema changes — uses ASP.NET Identity's existing password-reset token mechanism.
