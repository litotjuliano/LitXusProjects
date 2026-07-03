## Context

Two behaviors related to this same "close Features.md's auth gaps" pass turned out to already exist and work correctly (verified live, not just read from code): login already rejects deactivated users with a clear 401, and the refresh token already rotates on use with logout revoking it. Only password reset was a genuine gap — this change covers just that.

## Goals / Non-Goals

**Goals:**
- Give an Admin/Super Admin a way to help a locked-out user without email infrastructure.
- Close the broken frontend path (`RecoverPassword.tsx` calling a nonexistent endpoint) honestly rather than leaving it half-wired.

**Non-Goals:**
- Self-service "forgot password" via email — no SMTP infrastructure exists in this project at all; explicitly deferred, not attempted with an insecure workaround.

## Decisions

- **`GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` called back-to-back in the same server-side method**, not exposed as two separate steps — there's no reason to split "request" and "confirm" into two HTTP round trips when the same authenticated Admin call handles both; splitting them would only reintroduce the "token needs to travel somewhere" problem this design avoids.
- **Reuses `Admin.Users.Update`** (not a new permission) — same reasoning as `CreateUserCommandHandler`'s decision: one more user-management action under the existing umbrella, not a new capability needing its own permission row.
- **Same Super Admin exclusion pattern as `CreateUserAsync`/`AssignRoleCommandHandler`** — `Admin.Users.Update` is granted to the whole Admin role (not just Super Admin), so without this guard any Admin could reset the install owner's password and take over the account. Verified live: a Super Admin's own reset attempt against their own account (self-service edge case within Admin-initiated reset) is correctly rejected with 403.
- **`RecoverPassword.tsx` becomes purely informational** (no form, no redux dispatch) rather than deleting the route or the underlying `FORGOT_PASSWORD` redux action/saga/reducer scaffolding — the page's only real consumer (the broken form) is gone, but ripping the action out of 6 interconnected redux files for unreachable dead code is a disproportionate refactor for this change; left as inert, harmless scaffolding (not unlike other unused Konrix-template pieces already in this codebase).

## Risks / Trade-offs

- [Admin sees/sets the new password in plaintext, shares it out-of-band] → Mitigation: identical trade-off already accepted for `CreateUserAsync` (documented in the admin-driven-user-creation change's own design.md) — same trust model, same acceptable-for-now reasoning.
- [Dead `FORGOT_PASSWORD` redux scaffolding left in place] → Mitigation: genuinely unreachable (its only caller is gone), zero runtime risk; a cheap future cleanup, not urgent.
