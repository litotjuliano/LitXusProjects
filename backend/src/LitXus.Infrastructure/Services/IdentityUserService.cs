using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using LitXus.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Services;

public class IdentityUserService(UserManager<AppUser> userManager, IAppDbContext db) : IIdentityUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await db.AppUserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(db.AppRoles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        return users.Select(u => new UserSummaryDto(
            u.Id,
            u.FullName,
            u.Email ?? string.Empty,
            u.IsActive,
            userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Name).ToList(),
            u.LastLoginAtUtc)).ToList();
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        user.IsActive = isActive;
        await userManager.UpdateAsync(user);
    }

    public async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.Email;
    }
}
