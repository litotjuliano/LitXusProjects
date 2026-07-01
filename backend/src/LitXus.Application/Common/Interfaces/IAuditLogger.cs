namespace LitXus.Application.Common.Interfaces;

/// <summary>
/// For semantically-meaningful transitions a handler wants to record explicitly (e.g. "Approve" when
/// posting a GL entry) in addition to whatever AuditSaveChangesInterceptor captures automatically.
/// See docs/07_Audit_Trail.md §7.8.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? before,
        object? after,
        string? reason,
        CancellationToken cancellationToken = default);
}
