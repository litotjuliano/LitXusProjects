using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines).ThenInclude(l => l.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);

        return invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}
