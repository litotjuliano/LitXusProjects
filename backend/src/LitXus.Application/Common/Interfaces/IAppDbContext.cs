using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Identity.Entities;
using LitXus.Domain.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Common.Interfaces;

/// <summary>
/// Application-layer view of the persistence context — implemented by Infrastructure's AppDbContext.
/// Keeps handlers testable without an EF Core dependency leaking into Application (docs/01_Architecture.md §1.2).
/// </summary>
public interface IAppDbContext
{
    // Identity / RBAC / Shared
    // Named AppRoles/AppUserRoles (not Roles/UserRoles) to avoid colliding with the DbSets
    // IdentityDbContext already exposes for its own coarse IdentityRole/IdentityUserRole —
    // see docs/06_RBAC_Auth.md §6.1 for why both the Identity roles and our RBAC roles exist.
    DbSet<Role> AppRoles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> AppUserRoles { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<License> Licenses { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Company> Companies { get; }
    DbSet<CompanySignatory> CompanySignatories { get; }

    // Accounting
    DbSet<Account> Accounts { get; }
    DbSet<GLEntry> GLEntries { get; }
    DbSet<GLEntryLine> GLEntryLines { get; }
    DbSet<TaxCode> TaxCodes { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankStatementLine> BankStatementLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
