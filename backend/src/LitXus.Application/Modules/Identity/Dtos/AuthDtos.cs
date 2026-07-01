namespace LitXus.Application.Modules.Identity.Dtos;

public record UserSessionDto(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> EnabledModules);

public record LoginResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserSessionDto User);
