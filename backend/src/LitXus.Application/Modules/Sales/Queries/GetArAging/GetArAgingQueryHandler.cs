using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetArAging;

public class GetArAgingQueryHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<GetArAgingQuery, ArAgingDto>
{
    public async Task<ArAgingDto> Handle(GetArAgingQuery request, CancellationToken cancellationToken)
    {
        var asOfDate = request.AsOfDate ?? DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        var outstandingInvoices = await db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
            .ToListAsync(cancellationToken);

        var customerIds = outstandingInvoices.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var lines = outstandingInvoices
            .GroupBy(i => i.CustomerId)
            .Select(g =>
            {
                decimal current = 0, days1To30 = 0, days31To60 = 0, days61To90 = 0, over90Days = 0;
                foreach (var invoice in g)
                {
                    var daysOverdue = asOfDate.DayNumber - invoice.DueDate.DayNumber;
                    if (daysOverdue <= 0) current += invoice.OutstandingBalance;
                    else if (daysOverdue <= 30) days1To30 += invoice.OutstandingBalance;
                    else if (daysOverdue <= 60) days31To60 += invoice.OutstandingBalance;
                    else if (daysOverdue <= 90) days61To90 += invoice.OutstandingBalance;
                    else over90Days += invoice.OutstandingBalance;
                }

                var customer = customers[g.Key];
                var total = current + days1To30 + days31To60 + days61To90 + over90Days;
                return new ArAgingLineDto(customer.Id, customer.Code, customer.CompanyName, current, days1To30, days31To60, days61To90, over90Days, total);
            })
            .OrderByDescending(l => l.Total)
            .ToList();

        return new ArAgingDto(asOfDate, lines, lines.Sum(l => l.Total));
    }
}
