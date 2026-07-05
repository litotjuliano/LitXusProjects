using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetPayments;

public class GetPaymentsQueryHandler(IAppDbContext db) : IRequestHandler<GetPaymentsQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<PaymentStatus>(request.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync(cancellationToken);
        var invoiceIds = payments.Select(p => p.InvoiceId).Distinct().ToList();
        var invoiceNumbers = await db.Invoices.AsNoTracking()
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, cancellationToken);

        return payments.Select(p => p.ToDto(invoiceNumbers.GetValueOrDefault(p.InvoiceId))).ToList();
    }
}
