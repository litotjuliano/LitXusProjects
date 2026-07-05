using LitXus.Domain.Modules.Sales.Entities;

namespace LitXus.Application.Modules.Sales.Dtos;

public record CreditNoteDto(
    Guid Id,
    string? CreditNoteNumber,
    Guid InvoiceId,
    string? InvoiceNumber,
    string Reason,
    decimal Amount,
    string Status);

public static class CreditNoteMappingExtensions
{
    public static CreditNoteDto ToDto(this CreditNote creditNote, string? invoiceNumber) => new(
        creditNote.Id, creditNote.CreditNoteNumber, creditNote.InvoiceId, invoiceNumber,
        creditNote.Reason, creditNote.Amount, creditNote.Status.ToString());
}
