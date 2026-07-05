using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetInvoices;

public class GetInvoicesQueryHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Invoices.AsNoTracking().Include(i => i.Lines).ThenInclude(l => l.TaxCode).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<InvoiceStatus>(request.Status, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= request.DateTo.Value);
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync(cancellationToken);
        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        return invoices.Select(i => i.ToDto(customers[i.CustomerId], today)).ToList();
    }
}
