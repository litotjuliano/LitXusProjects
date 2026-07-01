using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Identity.Entities;
using LitXus.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Seeds two demo accounts so a fresh local/demo install has something to log in with.
/// Password is a fixed dev-only value — see docs/08_Sample_Data.md §8.2 for the pattern this
/// follows (never seeded when Seeding:Enabled is false, i.e. never in Production).
/// </summary>
public class UserSeeder(IAppDbContext db, UserManager<AppUser> userManager) : ISeeder
{
    private const string DemoPassword = "Demo@12345";

    public int Order => 3;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        await CreateUserAsync("superadmin@litxus.demo", "Super Admin Demo", "Super Admin", cancellationToken);
        await CreateUserAsync("admin@litxus.demo", "Admin Demo", "Admin", cancellationToken);
    }

    private async Task CreateUserAsync(string email, string fullName, string roleName, CancellationToken cancellationToken)
    {
        var user = new AppUser { UserName = email, Email = email, FullName = fullName, IsActive = true };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user {email}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
        }

        var role = await db.AppRoles.FirstAsync(r => r.Name == roleName, cancellationToken);
        db.AppUserRoles.Add(UserRole.Create(user.Id, role.Id));
        await db.SaveChangesAsync(cancellationToken);
    }
}
