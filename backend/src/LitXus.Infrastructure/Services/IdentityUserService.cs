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

    public async Task ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        var roleNames = await db.AppUserRoles.Where(ur => ur.UserId == userId)
            .Join(db.AppRoles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(cancellationToken);

        // Same protection as CreateUserAsync/AssignRoleCommandHandler — otherwise any Admin
        // (Admin.Users.Update is granted to the whole Admin role, not just Super Admin) could
        // reset the install owner's password and take over the account.
        if (roleNames.Contains("Super Admin"))
        {
            throw new ForbiddenException("The Super Admin account's password cannot be reset through this endpoint.");
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure("newPassword", e.Description)));
        }

        await auditLogger.LogAsync("User", user.Id.ToString(), "ResetPassword", null, null, null, cancellationToken);
    }
}
