using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetBalanceSheet;

public record GetBalanceSheetQuery(DateOnly AsOfDate) : IRequest<BalanceSheetDto>;
