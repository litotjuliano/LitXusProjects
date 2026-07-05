using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.RecordPayment;

public class RecordPaymentCommandHandler(IAppDbContext db) : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var payment = Payment.Create(
            request.InvoiceId, request.PaymentDate, request.Amount,
            Enum.Parse<PaymentMethod>(request.Method), request.ReferenceNumber, request.BankAccountId);

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return payment.ToDto(invoice.InvoiceNumber);
    }
}
