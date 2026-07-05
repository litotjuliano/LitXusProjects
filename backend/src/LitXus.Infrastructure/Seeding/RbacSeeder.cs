using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Seeding;
using LitXus.Domain.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Seeds the Permissions catalog and the role set. "Super Admin" and "Admin" are two distinct
/// tiers: Super Admin is the install owner (also manages the license and feature flags), Admin
/// is a full business administrator (everything except license/feature-flag management). The
/// other four roles (Accountant, SalesUser, InventoryManager, Manager, Viewer) match the matrix
/// in docs/06_RBAC_Auth.md §6.2. InventoryManager gets no grants yet since Inventory doesn't
/// exist until Phase 3.
///
/// Runs in every environment (AlwaysRun), including production where Seeding:Enabled is false —
/// this is reference/lookup data the app cannot function without, not demo data. Without it, a
/// fresh production install's Roles table stays empty and the first self-registered user (see
/// IdentityService.RegisterAsync) would silently get no role at all.
/// </summary>
public class RbacSeeder(IAppDbContext db) : ISeeder
{
    public int Order => 1;
    public bool AlwaysRun => true;

    /// <summary>
    /// Additive on every run: earlier phases already seeded Permissions/Roles, so this must not
    /// short-circuit just because the tables are non-empty (that left Phase 2's Sales.* permissions
    /// and role grants never inserted into any already-seeded database — caught via live testing).
    /// Existing permissions/roles are reused and re-granting an already-held permission is a no-op
    /// (Role.GrantPermission checks first), so running this every startup is safe and is exactly
    /// how a new phase's permissions reach an existing install.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var existingPermissions = await db.Permissions.ToListAsync(cancellationToken);
        var byCode = existingPermissions.ToDictionary(p => p.Code);

        var newPermissions = PermissionCatalog.All
            .Where(p => !byCode.ContainsKey(p.Code))
            .Select(p => Permission.Create(p.Module, p.Entity, p.Operation))
            .ToList();
        if (newPermissions.Count > 0)
        {
            db.Permissions.AddRange(newPermissions);
            foreach (var p in newPermissions) byCode[p.Code] = p;
        }

        var existingRoles = await db.AppRoles.Include(r => r.RolePermissions).ToDictionaryAsync(r => r.Name, cancellationToken);
        Role GetOrCreateRole(string name, string description)
        {
            if (existingRoles.TryGetValue(name, out var role)) return role;
            role = Role.Create(name, description);
            db.AppRoles.Add(role);
            existingRoles[name] = role;
            return role;
        }

        var superAdmin = GetOrCreateRole("Super Admin", "Install owner — full system access including license and feature flags.");
        var admin = GetOrCreateRole("Admin", "Full business administrator.");
        var accountant = GetOrCreateRole("Accountant", "GL entries, tax, reports.");
        var salesUser = GetOrCreateRole("SalesUser", "Sales and invoices (Phase 2+).");
        GetOrCreateRole("InventoryManager", "Stock management (Phase 3+).");
        var manager = GetOrCreateRole("Manager", "Read-only reports across modules.");
        var viewer = GetOrCreateRole("Viewer", "Read-only access.");

        var permissions = byCode.Values.ToList();
        foreach (var p in permissions)
        {
            superAdmin.GrantPermission(p);
        }

        foreach (var p in permissions.Where(p => p.Code is not ("Admin.License.Read" or "Admin.License.Update")))
        {
            admin.GrantPermission(p);
        }

        foreach (var code in new[]
        {
            "Accounting.Account.Create", "Accounting.Account.Read", "Accounting.Account.Update",
            "Accounting.GLEntry.Create", "Accounting.GLEntry.Read", "Accounting.GLEntry.Update", "Accounting.GLEntry.Approve",
            "Accounting.TaxCode.Read", "Accounting.TaxCode.Create",
            "Accounting.BankAccount.Read", "Accounting.BankAccount.Create", "Accounting.BankAccount.Update",
            "Accounting.Reports.Read", "Accounting.Reports.Export",
            // Accountant: Sales Read only (docs/06_RBAC_Auth.md §6.2 matrix).
            "Sales.Customer.Read", "Sales.Invoice.Read", "Sales.Payment.Read", "Sales.CreditNote.Read", "Sales.Reports.Read",
        })
        {
            accountant.GrantPermission(byCode[code]);
        }

        // SalesUser: Create/Read/Update Invoices (and Customers/Payments/CreditNotes), no Approve —
        // matches the matrix's "no Approve" note exactly (Invoice.Approve gates both Issue and
        // Void; Payment.Verify is Admin-only per the Phase 2 testing checklist).
        foreach (var code in new[]
        {
            "Sales.Customer.Create", "Sales.Customer.Read", "Sales.Customer.Update",
            "Sales.Invoice.Create", "Sales.Invoice.Read", "Sales.Invoice.Update",
            "Sales.Payment.Create", "Sales.Payment.Read",
            "Sales.CreditNote.Create", "Sales.CreditNote.Read",
            "Sales.Reports.Read",
        })
        {
            salesUser.GrantPermission(byCode[code]);
        }

        manager.GrantPermission(byCode["Accounting.Reports.Read"]);
        manager.GrantPermission(byCode["Sales.Reports.Read"]);
        manager.GrantPermission(byCode["Admin.AuditLogs.Read"]);

        foreach (var code in new[]
        {
            "Accounting.Account.Read", "Accounting.GLEntry.Read", "Accounting.TaxCode.Read",
            "Accounting.BankAccount.Read", "Accounting.Reports.Read",
            "Sales.Customer.Read", "Sales.Invoice.Read", "Sales.Payment.Read", "Sales.CreditNote.Read", "Sales.Reports.Read",
        })
        {
            viewer.GrantPermission(byCode[code]);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
