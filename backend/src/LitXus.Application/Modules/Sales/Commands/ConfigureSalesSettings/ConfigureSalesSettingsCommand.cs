using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.ConfigureSalesSettings;

public record ConfigureSalesSettingsCommand(
    Guid ReceivableAccountId,
    Guid RevenueAccountId,
    Guid TaxPayableAccountId,
    Guid CashAccountId) : IRequest<SalesSettingsDto>;
