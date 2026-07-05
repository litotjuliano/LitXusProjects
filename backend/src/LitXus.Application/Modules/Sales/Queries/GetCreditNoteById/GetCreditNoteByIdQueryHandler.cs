using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetCreditNoteById;

public class GetCreditNoteByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetCreditNoteByIdQuery, CreditNoteDto>
{
    public async Task<CreditNoteDto> Handle(GetCreditNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var creditNote = await db.CreditNotes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CreditNote), request.Id);

        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == creditNote.InvoiceId, cancellationToken);

        return creditNote.ToDto(invoice?.InvoiceNumber);
    }
}
