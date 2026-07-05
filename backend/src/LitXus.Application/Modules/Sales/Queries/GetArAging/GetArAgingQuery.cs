using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetArAging;

public record GetArAgingQuery(DateOnly? AsOfDate = null) : IRequest<ArAgingDto>;
