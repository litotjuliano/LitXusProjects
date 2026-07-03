using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetTaxCodes;

public class GetTaxCodesQueryHandler(IAppDbContext db) : IRequestHandler<GetTaxCodesQuery, IReadOnlyList<TaxCodeDto>>
{
    public async Task<IReadOnlyList<TaxCodeDto>> Handle(GetTaxCodesQuery request, CancellationToken cancellationToken)
    {
        var taxCodes = await db.TaxCodes.AsNoTracking().OrderBy(t => t.Code).ToListAsync(cancellationToken);
        return taxCodes.Select(t => t.ToDto()).ToList();
    }
}
