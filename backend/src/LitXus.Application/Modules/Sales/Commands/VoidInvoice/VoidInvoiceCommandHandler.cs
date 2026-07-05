using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.VoidInvoice;

public class VoidInvoiceCommandHandler(IAppDbContext db, IAuditLogger auditLogger, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<VoidInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(VoidInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines).ThenInclude(l => l.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);

        var hasVerifiedPayment = await db.Payments.AnyAsync(
            p => p.InvoiceId == invoice.Id && p.Status == PaymentStatus.Verified, cancellationToken);

        var before = new { invoice.Status };
        invoice.Void(request.Reason, hasVerifiedPayment);

        await auditLogger.LogAsync(
            nameof(Invoice), invoice.Id.ToString(), "Void",
            before, new { invoice.Status }, request.Reason, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}
