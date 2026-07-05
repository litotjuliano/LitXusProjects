using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetSalesSummary;

public record GetSalesSummaryQuery(DateOnly From, DateOnly To, string GroupBy) : IRequest<SalesSummaryDto>;
