using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Company.Queries.GetCompanyProfile;

public class GetCompanyProfileQueryHandler(IAppDbContext db) : IRequestHandler<GetCompanyProfileQuery, CompanyDto?>
{
    public async Task<CompanyDto?> Handle(GetCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return company?.ToDto();
    }
}
