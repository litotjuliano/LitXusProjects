namespace LitXus.Application.Modules.Identity.Dtos;

public record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime? LastLoginAtUtc);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions);

public record PermissionDto(
    Guid Id,
    string Module,
    string Entity,
    string Operation,
    string Code);

public record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string? BeforeValuesJson,
    string? AfterValuesJson,
    string? Reason,
    Guid? UserId,
    string? UserEmail,
    string? IpAddress,
    DateTime TimestampUtc);
