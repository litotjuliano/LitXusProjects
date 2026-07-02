using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Licensing.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LicenseEntity = LitXus.Domain.Modules.Shared.Entities.License;

namespace LitXus.Application.Modules.Licensing.Queries.GetLicense;

public class GetLicenseQueryHandler(IAppDbContext db) : IRequestHandler<GetLicenseQuery, LicenseDto>
{
    public async Task<LicenseDto> Handle(GetLicenseQuery request, CancellationToken cancellationToken)
    {
        var license = await db.Licenses.AsNoTracking().OrderByDescending(l => l.IssuedAtUtc).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(LicenseEntity), "current");

        return license.ToDto();
    }
}
