using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetCreditNotes;

public class GetCreditNotesQueryHandler(IAppDbContext db) : IRequestHandler<GetCreditNotesQuery, IReadOnlyList<CreditNoteDto>>
{
    public async Task<IReadOnlyList<CreditNoteDto>> Handle(GetCreditNotesQuery request, CancellationToken cancellationToken)
    {
        var creditNotes = await db.CreditNotes.AsNoTracking().OrderByDescending(c => c.CreditNoteNumber).ToListAsync(cancellationToken);
        var invoiceIds = creditNotes.Select(c => c.InvoiceId).Distinct().ToList();
        var invoiceNumbers = await db.Invoices.AsNoTracking()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);

        return creditNotes.Select(c => c.ToDto(invoiceNumbers.GetValueOrDefault(c.InvoiceId))).ToList();
    }
}
