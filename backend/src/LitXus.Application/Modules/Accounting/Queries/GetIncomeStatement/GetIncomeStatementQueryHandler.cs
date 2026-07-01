using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetIncomeStatement;

public class GetIncomeStatementQueryHandler(IAppDbContext db) : IRequestHandler<GetIncomeStatementQuery, IncomeStatementDto>
{
    public async Task<IncomeStatementDto> Handle(GetIncomeStatementQuery request, CancellationToken cancellationToken)
    {
        var activity = await (
            from line in db.GLEntryLines.AsNoTracking()
            join entry in db.GLEntries.AsNoTracking() on line.GLEntryId equals entry.Id
            join account in db.Accounts.AsNoTracking() on line.AccountId equals account.Id
            where entry.Status == GLEntryStatus.Posted
                  && entry.EntryDate >= request.From && entry.EntryDate <= request.To
                  && (account.Type == AccountType.Revenue || account.Type == AccountType.Expense)
            group new { line, account } by new { account.Id, account.Code, account.Name, account.Type } into g
            select new
            {
                g.Key.Code,
                g.Key.Name,
                g.Key.Type,
                TotalDebit = g.Sum(x => x.line.DebitAmount),
                TotalCredit = g.Sum(x => x.line.CreditAmount),
            }
        ).ToListAsync(cancellationToken);

        // Revenue is credit-normal (credit - debit); Expense is debit-normal (debit - credit).
        var revenueLines = activity.Where(a => a.Type == AccountType.Revenue)
            .Select(a => new IncomeStatementLineDto(a.Code, a.Name, a.TotalCredit - a.TotalDebit))
            .OrderBy(l => l.AccountCode)
            .ToList();

        var expenseLines = activity.Where(a => a.Type == AccountType.Expense)
            .Select(a => new IncomeStatementLineDto(a.Code, a.Name, a.TotalDebit - a.TotalCredit))
            .OrderBy(l => l.AccountCode)
            .ToList();

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Amount);

        return new IncomeStatementDto(
            request.From, request.To, revenueLines, expenseLines, totalRevenue, totalExpenses, totalRevenue - totalExpenses);
    }
}
