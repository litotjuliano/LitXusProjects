using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using LitXus.Domain.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Identity;

public class AuthenticationException(string message) : Exception(message);

/// <summary>
/// Auth is deliberately kept as a direct service (not MediatR commands) — it's inherently
/// framework-bound to ASP.NET Identity's UserManager, and routing it through the same
/// Application-layer abstraction as business commands wouldn't buy testability here.
/// Refresh tokens are stored via Identity's own AspNetUserTokens store (AuthenticationToken),
/// not a custom table — see docs/06_RBAC_Auth.md §6.3-6.4.
/// </summary>
public class IdentityService(
    UserManager<AppUser> userManager,
    IFeatureFlagService featureFlagService,
    JwtTokenGenerator jwtTokenGenerator,
    IAppDbContext db,
    IDateTimeProvider dateTimeProvider)
{
    private const string RefreshTokenProvider = "LitXus";
    private const string RefreshTokenName = "RefreshToken";

    /// <summary>
    /// Self-registration is bootstrap-only: it works exactly once, for the very first user on a
    /// fresh install (including a fresh production install, where nothing is seeded — see
    /// RbacSeeder.AlwaysRun), who is auto-activated as Super Admin — they're the literal install
    /// owner and need license authority immediately, and no other account could grant them Super
    /// Admin afterward (self-escalation is deliberately blocked). Every user after that is created
    /// by an Admin/Super Admin directly (CreateUserCommand), not by self-registering — this method
    /// rejects the call once any user already exists.
    /// </summary>
    public async Task<AppUser> RegisterAsync(string email, string password, string fullName)
    {
        var isFirstUser = !await userManager.Users.AnyAsync();
        if (!isFirstUser)
        {
            throw new AuthenticationException("Self-registration is disabled. Ask an administrator to create your account.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new AuthenticationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        var superAdminRole = await db.AppRoles.FirstOrDefaultAsync(r => r.Name == "Super Admin");
        if (superAdminRole is not null)
        {
            db.AppUserRoles.Add(UserRole.Create(user.Id, superAdminRole.Id));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        return user;
    }

    public async Task<LoginResultDto> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new AuthenticationException("Invalid credentials.");

        if (!user.IsActive)
        {
            throw new AuthenticationException("This account is not active.");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            throw new AuthenticationException("Invalid credentials.");
        }

        user.LastLoginAtUtc = dateTimeProvider.UtcNow;
        await userManager.UpdateAsync(user);

        return await IssueSessionAsync(user);
    }

    public async Task<LoginResultDto> RefreshAsync(string refreshToken)
    {
        // AspNetUserTokens has no lookup-by-value index, so this scans active users' stored
        // tokens — acceptable at Phase 1 scale; revisit with a dedicated indexed table if
        // refresh volume grows enough to matter.
        foreach (var user in await userManager.Users.Where(u => u.IsActive).ToListAsync())
        {
            var stored = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            if (stored == refreshToken)
            {
                return await IssueSessionAsync(user);
            }
        }

        throw new AuthenticationException("Invalid or expired refresh token.");
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        }
    }

    public async Task<UserSessionDto> GetSessionAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new AuthenticationException("User not found.");

        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id);
        return BuildSessionDto(user, roles, permissions);
    }

    private async Task<LoginResultDto> IssueSessionAsync(AppUser user)
    {
        var (roles, permissions) = await LoadRolesAndPermissionsAsync(user.Id);
        var enabledModules = featureFlagService.EnabledModules.Select(m => m.ToString()).ToList();

        var (accessToken, expiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user, roles, permissions, enabledModules);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);

        var sessionDto = BuildSessionDto(user, roles, permissions);
        var expiresIn = (int)(expiresAtUtc - DateTime.UtcNow).TotalSeconds;

        return new LoginResultDto(accessToken, refreshToken, expiresIn, sessionDto);
    }

    private async Task<(List<string> Roles, List<string> Permissions)> LoadRolesAndPermissionsAsync(Guid userId)
    {
        var roleIds = await db.AppUserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();
        var roles = await db.AppRoles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
        var permissionIds = await db.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId).Distinct().ToListAsync();
        var permissions = await db.Permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Code).ToListAsync();

        return (roles.Select(r => r.Name).ToList(), permissions);
    }

    private UserSessionDto BuildSessionDto(AppUser user, List<string> roles, List<string> permissions) => new(
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        roles,
        permissions,
        featureFlagService.EnabledModules.Select(m => m.ToString()).ToList());
}
