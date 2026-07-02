using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Company.Queries.GetSignatories;

public class GetSignatoriesQueryHandler(IAppDbContext db) : IRequestHandler<GetSignatoriesQuery, IReadOnlyList<CompanySignatoryDto>>
{
    public async Task<IReadOnlyList<CompanySignatoryDto>> Handle(GetSignatoriesQuery request, CancellationToken cancellationToken)
    {
        var signatories = await db.CompanySignatories.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return signatories.Select(s => s.ToDto()).ToList();
    }
}
