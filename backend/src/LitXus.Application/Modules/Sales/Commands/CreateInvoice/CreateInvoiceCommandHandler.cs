using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        if (!customer.IsActive)
        {
            throw new CustomerInactiveException(customer.Code);
        }

        var taxCodeIds = request.Lines.Where(l => l.TaxCodeId.HasValue).Select(l => l.TaxCodeId!.Value).Distinct().ToList();
        var taxCodes = await db.TaxCodes
            .Where(t => taxCodeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var lines = request.Lines.Select(l =>
        {
            TaxCode? taxCode = null;
            if (l.TaxCodeId.HasValue && !taxCodes.TryGetValue(l.TaxCodeId.Value, out taxCode))
            {
                throw new NotFoundException(nameof(TaxCode), l.TaxCodeId.Value);
            }

            return InvoiceLine.Create(l.Description, l.Quantity, l.UnitOfMeasure, l.UnitPrice, taxCode);
        }).ToList();

        var invoice = Invoice.CreateDraft(request.CustomerId, request.InvoiceDate, request.DueDate, request.Notes, lines);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}
