using LitXus.Domain.Modules.Sales.Entities;

namespace LitXus.Application.Modules.Sales.Dtos;

public record SalesSettingsDto(
    Guid? DefaultReceivableAccountId,
    Guid? DefaultRevenueAccountId,
    Guid? DefaultTaxPayableAccountId,
    Guid? DefaultCashAccountId,
    bool IsConfigured);

public static class SalesSettingsMappingExtensions
{
    public static SalesSettingsDto ToDto(this SalesSettings settings) => new(
        settings.DefaultReceivableAccountId, settings.DefaultRevenueAccountId,
        settings.DefaultTaxPayableAccountId, settings.DefaultCashAccountId, settings.IsConfigured);
}
