using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Application.Modules.Sales.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.RejectPayment;

public class RejectPaymentCommandHandler(IAppDbContext db) : IRequestHandler<RejectPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RejectPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken);

        payment.Reject(request.Reason);
        await db.SaveChangesAsync(cancellationToken);

        return payment.ToDto(invoice?.InvoiceNumber);
    }
}
