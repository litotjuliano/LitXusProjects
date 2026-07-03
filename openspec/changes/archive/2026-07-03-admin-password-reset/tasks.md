## 1. Backend

- [x] 1.1 `IIdentityUserService.ResetUserPasswordAsync` + implementation (Super Admin guard, audit log)
- [x] 1.2 `ResetUserPasswordCommand`/Handler/Validator
- [x] 1.3 `POST /admin/users/{id}/reset-password` on `AdminUsersController`

## 2. Frontend

- [x] 2.1 `resetUserPassword()` helper
- [x] 2.2 "Reset Password" button + modal on `Users.tsx`
- [x] 2.3 Rewrite `RecoverPassword.tsx` to informational-only content

## 3. Regression tests (auth behaviors already implemented, previously untested)

- [x] 3.1 `Login_WhenAccountDeactivated_Returns401WithClearMessage`
- [x] 3.2 `RefreshToken_RotatesOnUse_OldTokenIsRejectedAfterwards`
- [x] 3.3 `Logout_RevokesTheRefreshToken`

## 4. Documentation

- [x] 4.1 OpenSpec identity-auth spec (this change)
- [x] 4.2 `Features.md` — check off all 3 auth items (2 were already working, now locked in by tests)
- [x] 4.3 API spec docs — new endpoint
- [x] 4.4 Admin_Setup_User_Guide.md — Reset Password scenario

## 5. Verification

- [x] 5.1 Backend + frontend build clean, full test suite passes (68 tests)
- [x] 5.2 Live: reset a user's password via the UI, confirm login with the new password succeeds
- [x] 5.3 Live: attempt to reset the Super Admin's password directly via the API, confirm 403
- [x] 5.4 Live: deactivated-user login 401, refresh-token rotation, logout revocation all reconfirmed
