using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetCustomers;

public class GetCustomersQueryHandler(IAppDbContext db) : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var customers = await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
        return customers.Select(c => c.ToDto()).ToList();
    }
}
