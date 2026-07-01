using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.Shared;

public record AccountActivity(Account Account, decimal TotalDebit, decimal TotalCredit);

/// <summary>
/// Shared by Trial Balance and Balance Sheet, which are both "point-in-time balance" reports —
/// computed by summing Posted GLEntryLines rather than reading the denormalized Account.Balance
/// field, since Balance always reflects "now," not an arbitrary as-of date. Income Statement and
/// General Ledger have different enough shapes (period activity vs. per-account line detail) that
/// they're not folded into this helper.
/// </summary>
public static class AccountActivityHelper
{
    public static async Task<IReadOnlyList<AccountActivity>> GetBalancesAsOfAsync(
        IAppDbContext db, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Code).ToListAsync(cancellationToken);

        var activity = await (
            from line in db.GLEntryLines.AsNoTracking()
            join entry in db.GLEntries.AsNoTracking()
                on line.GLEntryId equals entry.Id
            where entry.Status == Domain.Modules.Accounting.Enums.GLEntryStatus.Posted
                  && entry.EntryDate <= asOfDate
            group line by line.AccountId into g
            select new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(l => l.DebitAmount),
                TotalCredit = g.Sum(l => l.CreditAmount),
            }
        ).ToListAsync(cancellationToken);

        return accounts.Select(a =>
        {
            var match = activity.FirstOrDefault(x => x.AccountId == a.Id);
            return new AccountActivity(a, match?.TotalDebit ?? 0m, match?.TotalCredit ?? 0m);
        }).ToList();
    }
}
