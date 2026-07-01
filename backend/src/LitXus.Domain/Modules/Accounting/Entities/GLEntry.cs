using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Accounting.Exceptions;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class GLEntry : BaseEntity, IAuditable
{
    private readonly List<GLEntryLine> _lines = [];

    public string? EntryNumber { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public GLEntrySource Source { get; private set; } = GLEntrySource.Manual;
    public Guid? SourceReferenceId { get; private set; }
    public GLEntryStatus Status { get; private set; } = GLEntryStatus.Draft;
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? PostedBy { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<GLEntryLine> Lines => _lines.AsReadOnly();

    private GLEntry() { }

    public static GLEntry CreateDraft(
        DateOnly entryDate,
        string description,
        IEnumerable<GLEntryLine> lines,
        GLEntrySource source = GLEntrySource.Manual,
        Guid? sourceReferenceId = null)
    {
        var entry = new GLEntry
        {
            EntryDate = entryDate,
            Description = description,
            Source = source,
            SourceReferenceId = sourceReferenceId,
        };
        entry.ReplaceLines(lines);
        return entry;
    }

    /// <summary>Only Draft entries are editable (docs/phase-1-accounting/Business_Rules.md).</summary>
    public void UpdateLines(DateOnly entryDate, string description, IEnumerable<GLEntryLine> lines)
    {
        EnsureIsDraft();
        EntryDate = entryDate;
        Description = description;
        ReplaceLines(lines);
    }

    private void ReplaceLines(IEnumerable<GLEntryLine> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
        {
            line.AttachTo(Id);
            _lines.Add(line);
        }
    }

    /// <summary>
    /// Assigns the sequential entry number and posts the entry, updating account balances.
    /// Number assignment happens here (not at creation) since Draft entries may never be posted —
    /// see docs/phase-1-accounting/Business_Rules.md "Entry numbers are sequential and gap-free".
    /// </summary>
    public void Post(string entryNumber, Guid postedBy, DateTime postedAtUtc)
    {
        EnsureIsDraft();

        if (_lines.Count < 2)
        {
            throw new EntryTooFewLinesException();
        }

        var totalDebit = _lines.Sum(l => l.DebitAmount);
        var totalCredit = _lines.Sum(l => l.CreditAmount);
        if (totalDebit != totalCredit)
        {
            throw new EntryUnbalancedException(totalDebit, totalCredit);
        }
        // A zero-value entry (all lines RM 0.00) is technically balanced and allowed to post —
        // see docs/phase-1-accounting/Test_Scenarios.md "GL Entries > Edge cases".

        foreach (var line in _lines)
        {
            if (!line.Account.IsActive)
            {
                throw new AccountInactiveException(line.Account.Code);
            }
        }

        foreach (var line in _lines)
        {
            if (line.DebitAmount > 0) line.Account.ApplyDebit(line.DebitAmount);
            if (line.CreditAmount > 0) line.Account.ApplyCredit(line.CreditAmount);
        }

        EntryNumber = entryNumber;
        Status = GLEntryStatus.Posted;
        PostedAtUtc = postedAtUtc;
        PostedBy = postedBy;
    }

    /// <summary>
    /// Voids a Posted entry, reversing its balance impact. The entry number is retained
    /// (never reused) per Malaysia compliance document-numbering requirements — see
    /// docs/15_Malaysia_Compliance.md §15.6.
    /// </summary>
    public void Void(string reason)
    {
        if (Status != GLEntryStatus.Posted)
        {
            throw new EntryNotDraftException();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new VoidRequiresReasonException();
        }

        foreach (var line in _lines)
        {
            if (line.DebitAmount > 0) line.Account.ApplyCredit(line.DebitAmount);
            if (line.CreditAmount > 0) line.Account.ApplyDebit(line.CreditAmount);
        }

        Status = GLEntryStatus.Voided;
        VoidReason = reason;
    }

    private void EnsureIsDraft()
    {
        if (Status != GLEntryStatus.Draft)
        {
            throw new EntryNotDraftException();
        }
    }
}
