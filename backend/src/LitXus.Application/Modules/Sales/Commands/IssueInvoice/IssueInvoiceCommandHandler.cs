using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler(IAppDbContext db, INumberSequenceGenerator numberSequenceGenerator, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<IssueInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines).ThenInclude(l => l.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);

        var invoiceNumber = await numberSequenceGenerator.NextInvoiceNumberAsync(cancellationToken);
        invoice.Issue(invoiceNumber);

        // GL posting happens via InvoiceIssuedEvent -> PostInvoiceToGLHandler (docs/01_Architecture.md
        // §1.4), dispatched by DomainEventDispatchInterceptor once this SaveChanges commits.
        await db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}
