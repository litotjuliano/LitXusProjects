## Context

`IdentityService.RegisterAsync` already had an `isFirstUser` branch (added in the previous "Production Distribution" change) that auto-activates the first-ever user as Super Admin. Everyone after that landed `Pending` with no role, requiring a manual API call to assign one — no UI existed for it, even though `assignRole`/`revokeRole` frontend helpers and backend endpoints already existed unused.

## Goals / Non-Goals

**Goals:**
- Make self-registration bootstrap-only; reject it once any user exists.
- Give Admin/Super Admin a one-step way to create a fully-usable account (name, email, password, role) without a Pending/activation detour.
- Wire the existing role-assignment endpoints into the Users page UI.

**Non-Goals:**
- Custom role creation, bulk invite (flagged separately, not part of this change).
- Email-based invite flow (no email infrastructure exists) — the Admin sets the initial password directly and shares it out-of-band.

## Decisions

- **Reuse `Admin.Users.Update` for the new create-user permission** rather than adding `Admin.Users.Create` — avoids a permission-catalog reseed for one closely-related action; RBAC granularity can be revisited later if a real need for splitting create/update arises.
- **`CreateUserCommandHandler` delegates to `IIdentityUserService.CreateUserAsync`** (Infrastructure), not `UserManager<AppUser>` directly, since Application layer can't reference ASP.NET Identity types — matches the existing `GetUsersAsync`/`SetUserActiveAsync` pattern on the same interface.
- **New accounts are `IsActive = true` immediately**, not Pending — an Admin creating the account already is the vouching step that Pending+activation used to represent for self-registered accounts.
- **Same Super Admin exclusion guard as `AssignRoleCommandHandler`**, duplicated into `CreateUserAsync` rather than extracted into a shared helper — two call sites doesn't yet justify the abstraction; revisit if a third appears.

## Risks / Trade-offs

- [Admin sets a user's initial password directly, seen in plaintext in the request] → Mitigation: matches how seeded demo accounts already work (fixed known passwords); no email/reset infrastructure exists yet to do better. Acceptable for the current single-tenant, admin-trusted-to-onboard-staff model.
- [Reusing `Admin.Users.Update` for create means an Admin who can edit users can also create them, with no way to separate those permissions] → Mitigation: matches the existing pattern (`Admin.Users.Update` already covers activate/deactivate and role assign/revoke) — creation is one more user-management action under the same umbrella, not a new capability.
