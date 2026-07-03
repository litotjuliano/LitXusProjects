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
/// in docs/06_RBAC_Auth.md §6.2 — SalesUser/InventoryManager get no grants yet since Sales/
/// Inventory don't exist until Phase 2/3.
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

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Permissions.AnyAsync(cancellationToken))
        {
            return;
        }

        var permissions = PermissionCatalog.All
            .Select(p => Permission.Create(p.Module, p.Entity, p.Operation))
            .ToList();
        db.Permissions.AddRange(permissions);

        var byCode = permissions.ToDictionary(p => p.Code);

        var superAdmin = Role.Create("Super Admin", "Install owner — full system access including license and feature flags.");
        var admin = Role.Create("Admin", "Full business administrator.");
        var accountant = Role.Create("Accountant", "GL entries, tax, reports.");
        var salesUser = Role.Create("SalesUser", "Sales and invoices (Phase 2+).");
        var inventoryManager = Role.Create("InventoryManager", "Stock management (Phase 3+).");
        var manager = Role.Create("Manager", "Read-only reports across modules.");
        var viewer = Role.Create("Viewer", "Read-only access.");
        db.AppRoles.AddRange(superAdmin, admin, accountant, salesUser, inventoryManager, manager, viewer);

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
        })
        {
            accountant.GrantPermission(byCode[code]);
        }

        manager.GrantPermission(byCode["Accounting.Reports.Read"]);
        manager.GrantPermission(byCode["Admin.AuditLogs.Read"]);

        foreach (var code in new[]
        {
            "Accounting.Account.Read", "Accounting.GLEntry.Read", "Accounting.TaxCode.Read",
            "Accounting.BankAccount.Read", "Accounting.Reports.Read",
        })
        {
            viewer.GrantPermission(byCode[code]);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
