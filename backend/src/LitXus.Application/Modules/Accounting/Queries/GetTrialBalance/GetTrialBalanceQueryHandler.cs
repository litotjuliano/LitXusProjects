using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Application.Modules.Accounting.Queries.Shared;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetTrialBalance;

public class GetTrialBalanceQueryHandler(IAppDbContext db) : IRequestHandler<GetTrialBalanceQuery, TrialBalanceDto>
{
    public async Task<TrialBalanceDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        var activity = await AccountActivityHelper.GetBalancesAsOfAsync(db, request.AsOfDate, cancellationToken);

        var lines = activity
            .Where(a => a.TotalDebit != 0 || a.TotalCredit != 0)
            .Select(a =>
            {
                var (debit, credit) = a.Account.GetTrialBalanceColumns(a.TotalDebit, a.TotalCredit);
                return new TrialBalanceLineDto(a.Account.Code, a.Account.Name, a.Account.Type.ToString(), debit, credit);
            })
            .ToList();

        return new TrialBalanceDto(
            request.AsOfDate,
            lines,
            lines.Sum(l => l.Debit),
            lines.Sum(l => l.Credit));
    }
}
