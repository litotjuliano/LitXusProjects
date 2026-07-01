using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetTrialBalance;

public record GetTrialBalanceQuery(DateOnly AsOfDate) : IRequest<TrialBalanceDto>;
