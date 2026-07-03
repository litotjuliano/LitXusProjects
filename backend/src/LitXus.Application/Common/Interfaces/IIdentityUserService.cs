using LitXus.Application.Modules.Identity.Dtos;

namespace LitXus.Application.Common.Interfaces;

/// <summary>
/// Application-layer view over ASP.NET Identity's UserManager — kept as its own interface
/// (rather than exposing AppUser/UserManager directly) so Application never references
/// Infrastructure's concrete Identity types. Implemented in Infrastructure/Services.
/// </summary>
public interface IIdentityUserService
{
    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new user, active immediately (no Pending state — the caller, an Admin/Super
    /// Admin, is the vouching step; there's no self-registration approval left to gate on).
    /// Throws ValidationException (Identity's CreateAsync errors — duplicate email, weak
    /// password) or ForbiddenException (roleId resolves to "Super Admin").
    /// </summary>
    Task<UserSummaryDto> CreateUserAsync(string email, string fullName, string password, Guid roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Admin-initiated only — there's no self-service "forgot password" flow, since that would
    /// require emailing a reset token to an anonymous caller with no email infrastructure in this
    /// project (see docs/06_RBAC_Auth.md). Generates and consumes an Identity password-reset token
    /// server-side in one call, so no token ever leaves the server. Throws NotFoundException,
    /// ForbiddenException (target user has the Super Admin role), or ValidationException
    /// (Identity's password-policy errors).
    /// </summary>
    Task ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
}
