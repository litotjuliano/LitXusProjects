namespace LitXus.Domain.Modules.Shared.Entities;

/// <summary>
/// Written only by AuditSaveChangesInterceptor / IAuditLogger — never updated or deleted by
/// application code (immutability also enforced at the DB permission level, see docs/07_Audit_Trail.md §7.4).
/// Does not implement IAuditable itself (would recurse).
/// </summary>
public class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? BeforeValuesJson { get; init; }
    public string? AfterValuesJson { get; init; }
    public string? Reason { get; init; }
    public Guid? UserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime TimestampUtc { get; init; }
}
