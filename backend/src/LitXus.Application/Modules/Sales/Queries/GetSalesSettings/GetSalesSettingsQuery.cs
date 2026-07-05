using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetSalesSettings;

public record GetSalesSettingsQuery : IRequest<SalesSettingsDto>;
