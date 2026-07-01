namespace LitXus.Domain.Common;

/// <summary>
/// Marker interface: entities implementing this are captured by AuditSaveChangesInterceptor
/// (see docs/07_Audit_Trail.md §7.3). Framework/lookup tables should not implement this.
/// </summary>
public interface IAuditable
{
}
