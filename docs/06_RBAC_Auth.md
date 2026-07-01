# 06 — RBAC & Authentication Design

## 6.1 Why Both ASP.NET Identity Roles AND a Custom Roles/Permissions Model

ASP.NET Core Identity ships `AspNetRoles`/`AspNetUserRoles`, but they're coarse (a user either is or isn't "Admin") and not database-editable at the granularity we need ("Accountant can Create+Read GL entries but not Approve them"). So:

- Identity's role claims are used only for the coarse `[Authorize(Roles = "Admin")]` gate on truly admin-only controllers.
- The real authorization decision is the custom `Roles` → `RolePermissions` → `Permissions` chain (schema in [02_Database_Schema.md](02_Database_Schema.md) §2.1), checked via a custom `RequirePermissionAttribute`. This is what the UI's role editor manages, and what makes permissions dynamic without a redeploy.

## 6.2 Permission Model

Format: `{Module}.{Entity}.{Operation}` — e.g. `Accounting.GLEntry.Create`, `Sales.Invoice.Approve`, `Inventory.StockMovement.Delete`, `Admin.Users.Update`.

**Example Role → Permission Matrix:**

| Role | Accounting | Sales | Inventory | Admin |
|---|---|---|---|---|
| Super Admin | Create/Read/Update/Delete/Approve | same | same | full, including License + FeatureFlags |
| Admin | Create/Read/Update/Delete/Approve | same | same | full except License management |
| Accountant | Create/Read/Update/Approve GL, Read reports | Read only | Read only | none |
| SalesUser | none | Create/Read/Update Invoices, no Approve | Read only (stock check) | none |
| InventoryManager | none | Read only | Create/Read/Update/Delete Stock | none |
| Manager | Read reports (all modules) | Read reports | Read reports | Read audit logs |
| Viewer | Read only, all modules | Read only | Read only | none |

**Super Admin vs. Admin** was added during Phase 1 implementation (not in the original 6-role plan): Super Admin is the install owner — the only role that can view/rotate the license key or toggle feature flags (`Admin.License.*`, `Admin.FeatureFlags.*`). Admin is a full business administrator with everything else. In a single-install product this distinction mostly matters for larger customers who want to separate "who runs the business" from "who can change what the software is licensed to do." Seeded automatically — see [08_Sample_Data.md](08_Sample_Data.md) §8.2.

## 6.3 JWT Token Structure & Lifecycle

```
Access Token (JWT, 30 min expiry)
  Claims: sub (userId), email, name, roles[], permissions[] (or a permission-version claim —
          see note below), enabledModules[], iat, exp

Refresh Token (opaque random string, 14 day expiry, stored hashed in AspNetUserTokens)
  Rotated on every use (old token invalidated the moment a new one is issued —
  detects token replay/theft)
```

**Design note on permissions-in-JWT:** Embedding the full permission list in the JWT is simplest but means a permission change doesn't take effect until the token expires (≤30 min — acceptable) or the user re-logs-in. If instant revocation is required later, swap to a `permissionsVersion` claim checked against a cached version number per user, invalidating on role change. Not needed for v1.0; documented here as the upgrade path.

## 6.4 Login/Logout Flow

```
1. POST /auth/login { email, password }
2. Identity validates password hash (PBKDF2, Identity default)
3. If IsActive=false or membership pending -> 403 with clear message
4. On success: generate access token (JWT) + refresh token (store hash + expiry)
5. Response: { accessToken, refreshToken, expiresIn, user: { id, name, roles, permissions, enabledModules } }
6. Frontend: authStore (Zustand) persists tokens to memory + refreshToken to httpOnly-cookie-equivalent
   (for SPA served from same origin as API, refresh token as httpOnly Secure cookie is preferred over
   localStorage to reduce XSS exposure)
7. Axios interceptor attaches Authorization: Bearer <accessToken> to every request
8. On 401 response: interceptor calls /auth/refresh once, retries original request; if refresh also
   fails, logs out and redirects to /login
9. POST /auth/logout: revokes current refresh token server-side, frontend clears authStore
```

## 6.5 Authorization Checks — Implementation

**Backend (defense-in-depth, checked in this order per request):**

```csharp
[ApiController]
[Route("api/v1/accounting/gl-entries")]
public class GLEntriesController : ControllerBase
{
    [HttpPost]
    [RequireModule(Module.Accounting)]                       // 1. is Accounting licensed & enabled?
    [RequirePermission("Accounting.GLEntry.Create")]          // 2. does the user's role grant this?
    public async Task<IActionResult> Create(CreateGLEntryRequest request)
    {
        var command = _mapper.Map<CreateGLEntryCommand>(request);
        var result = await _mediator.Send(command);           // 3. handler re-validates via pipeline
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

`RequirePermissionAttribute` reads the `permissions` claim from the validated JWT — no extra DB round-trip per request. `RequireModuleAttribute` checks `IFeatureFlagService`, which caches the `Licenses` row in memory (invalidated when admin toggles flags).

**Frontend guard:**

```tsx
// routes/ModuleGuard.tsx
export function ModuleGuard({ module, children }: { module: Module; children: ReactNode }) {
  const enabledModules = useAuthStore(s => s.enabledModules);
  if (!enabledModules.includes(module)) return <Navigate to="/dashboard" replace />;
  return children;
}

// usePermission hook, used to conditionally render buttons
export function usePermission(code: string) {
  const permissions = useAuthStore(s => s.permissions);
  return permissions.includes(code);
}

// usage
{usePermission('Accounting.GLEntry.Create') && <Button onClick={openCreateModal}>New Entry</Button>}
```

Frontend checks are UX only (hide what a user can't do) — the backend attribute pair is the actual security boundary. Never trust the frontend flag alone.

## 6.6 Database Schema for RBAC

See [02_Database_Schema.md](02_Database_Schema.md) §2.1: `Roles`, `Permissions`, `RolePermissions`, `UserRoles`. Seeded at Phase 1: the 6 roles from §6.2 above, and the full permission catalog generated from an enum in code (`Modules × Entities × Operations`) so it can never drift from what the code actually checks.
