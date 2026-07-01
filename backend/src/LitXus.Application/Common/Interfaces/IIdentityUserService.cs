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
}
