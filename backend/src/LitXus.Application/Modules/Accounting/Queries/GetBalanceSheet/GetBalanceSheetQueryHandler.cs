using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Application.Modules.Accounting.Queries.Shared;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetBalanceSheet;

/// <summary>
/// Phase 1 has no period-close mechanism (see docs/phase-1-accounting/Business_Rules.md), so
/// Revenue/Expense accounts are never formally closed into Retained Earnings. To keep
/// Assets = Liabilities + Equity balanced anyway, this computes a "Current Year Earnings" line
/// as (all-time Revenue - all-time Expense up to the as-of date) and folds it into Equity —
/// the standard simplified approach small-business accounting systems use without formal closing.
/// </summary>
public class GetBalanceSheetQueryHandler(IAppDbContext db) : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var activity = await AccountActivityHelper.GetBalancesAsOfAsync(db, request.AsOfDate, cancellationToken);

        BalanceSheetLineDto ToLine(AccountActivity a) => new(
            a.Account.Code, a.Account.Name, a.Account.ComputeNetBalance(a.TotalDebit, a.TotalCredit));

        var assets = activity.Where(a => a.Account.Type == AccountType.Asset && (a.TotalDebit != 0 || a.TotalCredit != 0))
            .Select(ToLine).ToList();
        var liabilities = activity.Where(a => a.Account.Type == AccountType.Liability && (a.TotalDebit != 0 || a.TotalCredit != 0))
            .Select(ToLine).ToList();
        var equity = activity.Where(a => a.Account.Type == AccountType.Equity && (a.TotalDebit != 0 || a.TotalCredit != 0))
            .Select(ToLine).ToList();

        var revenue = activity.Where(a => a.Account.Type == AccountType.Revenue)
            .Sum(a => a.Account.ComputeNetBalance(a.TotalDebit, a.TotalCredit));
        var expense = activity.Where(a => a.Account.Type == AccountType.Expense)
            .Sum(a => a.Account.ComputeNetBalance(a.TotalDebit, a.TotalCredit));
        var currentYearEarnings = revenue - expense;

        var totalAssets = assets.Sum(l => l.Balance);
        var totalLiabilitiesAndEquity = liabilities.Sum(l => l.Balance) + equity.Sum(l => l.Balance) + currentYearEarnings;

        return new BalanceSheetDto(
            request.AsOfDate, assets, liabilities, equity, currentYearEarnings, totalAssets, totalLiabilitiesAndEquity);
    }
}
