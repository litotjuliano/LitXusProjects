using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Queries.GetSalesSettings;

public class GetSalesSettingsQueryHandler(IAppDbContext db) : IRequestHandler<GetSalesSettingsQuery, SalesSettingsDto>
{
    public async Task<SalesSettingsDto> Handle(GetSalesSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await db.SalesSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return (settings ?? SalesSettings.CreateEmpty()).ToDto();
    }
}
