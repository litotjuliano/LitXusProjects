# 16 — Feature Flags & Product Packaging

## 16.1 How Feature Flags Work

A single `Licenses` table row per deployed instance (schema in [02_Database_Schema.md](02_Database_Schema.md) §2.1) stores `EnabledModules` as a CSV (`"Accounting,Sales,Inventory"`). `IFeatureFlagService` reads this once at startup, caches it in memory, and exposes:

```csharp
public interface IFeatureFlagService
{
    bool IsEnabled(Module module);
    IReadOnlyList<Module> EnabledModules { get; }
    void Invalidate();   // called after an admin toggles flags, forces re-read from DB
}

public enum Module { Accounting, Sales, Inventory }
```

## 16.2 Module Enable/Disable Logic

Three enforcement points, all backed by the same service (see [06_RBAC_Auth.md](06_RBAC_Auth.md) §6.5 for the full defense-in-depth chain):

1. **API:** `[RequireModule(Module.Accounting)]` action filter → 403 if disabled.
2. **Application:** command/query handlers also check (in case a request somehow bypasses the API filter, e.g. a future internal job) — belt and suspenders, not strictly redundant since it protects background/event-driven paths like GL auto-posting too.
3. **Frontend:** `ModuleGuard` route wrapper + nav rendering conditioned on `enabledModules` from the JWT claims.

Toggling a module off does not delete or hide existing data — historical GL entries remain queryable by an Admin even if Accounting is later disabled (e.g. downgrading from Enterprise Pro to Retail Pro); only *new* mutations through that module's endpoints are blocked.

## 16.3 How the Same Codebase Serves Three Products

There is no per-product build or branch. The binary is identical across all three products; `Licenses.EnabledModules` is the only variable:

| Product | EnabledModules value |
|---|---|
| LitXus Accounting Pro | `Accounting` |
| LitXus Retail Pro | `Sales,Inventory` |
| LitXus Enterprise Pro | `Accounting,Sales,Inventory` |

The GL auto-posting notification handlers ([01_Architecture.md](01_Architecture.md) §1.4) are additionally gated on **both** `Accounting` and `Sales`/`Inventory` being enabled simultaneously — so even an Enterprise Pro license only auto-posts once both sides of a given integration are on (relevant if an Admin temporarily disables Accounting for maintenance).

## 16.4 Licensing Logic

- `LicenseKey` is a signed token (e.g. JWT signed with a LitXus-held private key, verified with a public key embedded in the app) containing `ProductCode`, `EnabledModules`, `IssuedToCompany`, `ExpiresAtUtc`.
- On startup, the app validates the signature and expiry; on failure, the app still starts (so an expired customer isn't locked out of their own data) but operates in a **read-only, no-new-transactions** mode with a persistent banner — this is a business decision to avoid data-loss-by-lockout, documented here so it's an explicit choice rather than an oversight.
- License renewal: customer receives a new `LicenseKey`, pastes it into `Admin > License` screen, app re-validates and lifts the read-only restriction.

## 16.5 Configuration Per Product

No separate `appsettings.{Product}.json` files — `appsettings.Production.json` for a given customer's install simply has their license key. This keeps deployment identical across products (§10.3) — the only per-customer variable is the license, not the build artifact.

## 16.6 Example: Customer Enables Accounting + Sales Modules

```
1. Customer purchases Enterprise Pro but initially wants to license only Accounting + Sales
   (Inventory rolled out later as their business grows).
2. LitXus issues a LicenseKey with EnabledModules = "Accounting,Sales".
3. Admin pastes the key into Admin > License. IFeatureFlagService.Invalidate() is called,
   re-reading the DB-stored license record.
4. Frontend nav immediately hides Inventory; GET /api/v1/inventory/* now returns 403 for all users.
5. InvoicePostedEvent -> GL auto-posting handler is active (both Accounting and Sales enabled) —
   invoices immediately start generating GL entries.
6. Three months later, customer upgrades: new LicenseKey with EnabledModules = "Accounting,Sales,Inventory".
   Same process — Inventory reappears in nav, no data migration needed since the schema was
   always present, just gated.
```

This is the concrete mechanism behind the "how 3 products share one codebase" claim made in [01_Architecture.md](01_Architecture.md) §1.6 — worth reading together.
