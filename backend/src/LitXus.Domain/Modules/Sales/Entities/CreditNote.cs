using LitXus.Domain.Common;
using LitXus.Domain.Modules.Sales.Enums;

namespace LitXus.Domain.Modules.Sales.Entities;

/// <summary>
/// Single-step creation, not a Draft/Issue/Apply lifecycle — the 15-endpoint API surface only
/// exposes create + read (docs/03_API_Specification.md §3.6), and since a credit note is already
/// scoped to exactly one invoice at creation, "Issued" and "Applied" collapse into one real
/// transition. Status still carries all 3 documented values for schema fidelity.
/// </summary>
public class CreditNote : BaseEntity, IAuditable
{
    public string? CreditNoteNumber { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public CreditNoteStatus Status { get; private set; } = CreditNoteStatus.Draft;

    private CreditNote() { }

    public static CreditNote Create(string creditNoteNumber, Guid invoiceId, string reason, decimal amount)
    {
        return new CreditNote
        {
            CreditNoteNumber = creditNoteNumber,
            InvoiceId = invoiceId,
            Reason = reason,
            Amount = amount,
            Status = CreditNoteStatus.Applied,
        };
    }
}
