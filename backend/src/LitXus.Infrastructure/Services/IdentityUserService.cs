using FluentValidation.Results;
using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using LitXus.Domain.Modules.Identity.Entities;
using LitXus.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValidationException = LitXus.Application.Common.Exceptions.ValidationException;

namespace LitXus.Infrastructure.Services;

public class IdentityUserService(UserManager<AppUser> userManager, IAppDbContext db, IAuditLogger auditLogger) : IIdentityUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await db.AppUserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(db.AppRoles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new UserSummaryDto(
                u.Id,
                u.FullName,
                u.Email ?? string.Empty,
                u.IsActive,
                userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Name).ToList(),
                u.LastLoginAtUtc))
            // Super Admin is the install owner — excluded from the general Users list so it can't be
            // deactivated or otherwise managed there by anyone, including another Admin.
            .Where(u => !u.Roles.Contains("Super Admin"))
            .ToList();
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

    public async Task<UserSummaryDto> CreateUserAsync(string email, string fullName, string password, Guid roleId, CancellationToken cancellationToken)
    {
        var role = await db.AppRoles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), roleId);

        // Same protection as AssignRoleCommandHandler — Super Admin is provisioned only via the
        // one-time first-user bootstrap, never through an Admin-driven creation endpoint.
        if (role.Name == "Super Admin")
        {
            throw new ForbiddenException("The Super Admin role cannot be granted through this endpoint.");
        }

        var user = new AppUser { UserName = email, Email = email, FullName = fullName, IsActive = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure("password", e.Description)));
        }

        db.AppUserRoles.Add(UserRole.Create(user.Id, roleId));

        await auditLogger.LogAsync(
            "User", user.Id.ToString(), "Create",
            null, new { email, fullName }, null, cancellationToken);
        await auditLogger.LogAsync(
            "UserRole", user.Id.ToString(), "AssignRole",
            null, new { RoleId = roleId }, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new UserSummaryDto(user.Id, user.FullName, user.Email, user.IsActive, [role.Name], null);
    }
}
