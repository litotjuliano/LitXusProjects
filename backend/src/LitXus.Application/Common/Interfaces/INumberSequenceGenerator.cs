namespace LitXus.Application.Common.Interfaces;

/// <summary>
/// Sequential, gap-free document numbering (GL entries, and later invoices/credit notes).
/// Implemented via a SQL Server SEQUENCE object or sp_getapplock-guarded increment — never
/// MAX(number)+1 in application code, which is unsafe under concurrent posting.
/// See docs/phase-1-accounting/Business_Rules.md "Entry numbers are sequential and gap-free".
/// </summary>
public interface INumberSequenceGenerator
{
    Task<string> NextGLEntryNumberAsync(CancellationToken cancellationToken = default);
}
