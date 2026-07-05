using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.ConfigureSalesSettings;

public class ConfigureSalesSettingsCommandHandler(IAppDbContext db) : IRequestHandler<ConfigureSalesSettingsCommand, SalesSettingsDto>
{
    public async Task<SalesSettingsDto> Handle(ConfigureSalesSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.SalesSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = SalesSettings.CreateEmpty();
            db.SalesSettings.Add(settings);
        }

        settings.Configure(request.ReceivableAccountId, request.RevenueAccountId, request.TaxPayableAccountId, request.CashAccountId);
        await db.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }
}
