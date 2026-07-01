namespace LitXus.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    IReadOnlyList<string> Permissions { get; }
}
