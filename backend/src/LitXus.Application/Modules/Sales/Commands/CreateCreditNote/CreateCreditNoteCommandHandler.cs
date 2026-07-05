using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.CreateCreditNote;

/// <summary>
/// Reduces the invoice's outstanding balance exactly like a payment would (reusing
/// Invoice.ApplyPayment) — no GL entry is auto-posted for a credit note in this pass (out of
/// scope; not exercised by the Phase 2 testing checklist, unlike invoice issue/payment verify).
/// </summary>
public class CreateCreditNoteCommandHandler(IAppDbContext db, INumberSequenceGenerator numberSequenceGenerator)
    : IRequestHandler<CreateCreditNoteCommand, CreditNoteDto>
{
    public async Task<CreditNoteDto> Handle(CreateCreditNoteCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        if (request.Amount > invoice.OutstandingBalance)
        {
            throw new CreditNoteExceedsInvoiceBalanceException(invoice.OutstandingBalance);
        }

        var creditNoteNumber = await numberSequenceGenerator.NextCreditNoteNumberAsync(cancellationToken);
        var creditNote = CreditNote.Create(creditNoteNumber, request.InvoiceId, request.Reason, request.Amount);
        invoice.ApplyPayment(request.Amount);

        db.CreditNotes.Add(creditNote);
        await db.SaveChangesAsync(cancellationToken);

        return creditNote.ToDto(invoice.InvoiceNumber);
    }
}
