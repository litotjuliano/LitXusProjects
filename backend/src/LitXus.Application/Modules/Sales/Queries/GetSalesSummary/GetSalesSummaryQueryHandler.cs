using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetSalesSummary;

/// <summary>groupBy=product groups by InvoiceLine.Description (free text) — there's no real
/// Product entity to group by until Phase 3's Inventory module exists.</summary>
public class GetSalesSummaryQueryHandler(IAppDbContext db) : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    public async Task<SalesSummaryDto> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Void
                        && i.InvoiceDate >= request.From && i.InvoiceDate <= request.To)
            .ToListAsync(cancellationToken);

        List<SalesSummaryLineDto> lines;

        if (request.GroupBy == "product")
        {
            lines = invoices.SelectMany(i => i.Lines)
                .GroupBy(l => l.Description)
                .Select(g => new SalesSummaryLineDto(
                    g.Key, g.Sum(l => l.LineTotal), g.Sum(l => l.ComputeTaxAmount()),
                    g.Sum(l => l.LineTotal + l.ComputeTaxAmount()), g.Select(l => l.InvoiceId).Distinct().Count()))
                .OrderByDescending(l => l.TotalAmount)
                .ToList();
        }
        else if (request.GroupBy == "month")
        {
            lines = invoices.GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new SalesSummaryLineDto(
                    $"{g.Key.Year}-{g.Key.Month:D2}", g.Sum(i => i.SubTotal), g.Sum(i => i.SSTAmount), g.Sum(i => i.TotalAmount), g.Count()))
                .OrderBy(l => l.GroupKey)
                .ToList();
        }
        else
        {
            var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
            var customers = await db.Customers.AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            lines = invoices.GroupBy(i => i.CustomerId)
                .Select(g => new SalesSummaryLineDto(
                    $"{customers[g.Key].Code} {customers[g.Key].CompanyName}",
                    g.Sum(i => i.SubTotal), g.Sum(i => i.SSTAmount), g.Sum(i => i.TotalAmount), g.Count()))
                .OrderByDescending(l => l.TotalAmount)
                .ToList();
        }

        return new SalesSummaryDto(request.From, request.To, request.GroupBy, lines, lines.Sum(l => l.TotalAmount));
    }
}
