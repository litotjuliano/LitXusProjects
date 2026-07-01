using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Identity.Entities;
using LitXus.Domain.Modules.Shared.Entities;
using LitXus.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    // Identity / RBAC / Shared
    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> AppUserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Accounting
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<GLEntry> GLEntries => Set<GLEntry>();
    public DbSet<GLEntryLine> GLEntryLines => Set<GLEntryLine>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Soft-delete query filters — docs/02_Database_Schema.md intro ("nothing is hard-deleted")
        builder.Entity<Account>().HasQueryFilter(a => !a.IsDeleted);
        builder.Entity<GLEntry>().HasQueryFilter(e => !e.IsDeleted);

        // Backs NumberSequenceGenerator — gap-free GL entry numbering, never MAX(number)+1.
        builder.HasSequence<long>("GLEntryNumberSeq", schema: "dbo").StartsAt(1).IncrementsBy(1);
    }
}
