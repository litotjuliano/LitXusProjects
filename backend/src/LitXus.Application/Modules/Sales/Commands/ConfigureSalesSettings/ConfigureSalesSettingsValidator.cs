using FluentValidation;

namespace LitXus.Application.Modules.Sales.Commands.ConfigureSalesSettings;

public class ConfigureSalesSettingsValidator : AbstractValidator<ConfigureSalesSettingsCommand>
{
    public ConfigureSalesSettingsValidator()
    {
        RuleFor(x => x.ReceivableAccountId).NotEmpty();
        RuleFor(x => x.RevenueAccountId).NotEmpty();
        RuleFor(x => x.TaxPayableAccountId).NotEmpty();
        RuleFor(x => x.CashAccountId).NotEmpty();
    }
}
