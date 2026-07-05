using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.VerifyPayment;

public class VerifyPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<VerifyPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), payment.InvoiceId);

        // Applied to the invoice's balance first (throws if it would overpay) — only if that
        // succeeds does the payment itself flip to Verified, so the two never disagree.
        invoice.ApplyPayment(payment.Amount);
        payment.Verify(currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        // GL posting happens via PaymentVerifiedEvent -> PostPaymentToGLHandler (docs/01_Architecture.md
        // §1.4), dispatched by DomainEventDispatchInterceptor once this SaveChanges commits.
        await db.SaveChangesAsync(cancellationToken);

        return payment.ToDto(invoice.InvoiceNumber);
    }
}
