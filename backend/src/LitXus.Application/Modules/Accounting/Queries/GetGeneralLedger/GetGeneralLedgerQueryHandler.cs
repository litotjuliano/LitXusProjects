using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetGeneralLedger;

public class GetGeneralLedgerQueryHandler(IAppDbContext db) : IRequestHandler<GetGeneralLedgerQuery, GeneralLedgerDto>
{
    public async Task<GeneralLedgerDto> Handle(GetGeneralLedgerQuery request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var openingActivity = await (
            from line in db.GLEntryLines.AsNoTracking()
            join entry in db.GLEntries.AsNoTracking() on line.GLEntryId equals entry.Id
            where line.AccountId == request.AccountId
                  && entry.Status == GLEntryStatus.Posted
                  && entry.EntryDate < request.From
            select line
        ).ToListAsync(cancellationToken);

        var runningBalance = account.ComputeNetBalance(
            openingActivity.Sum(l => l.DebitAmount), openingActivity.Sum(l => l.CreditAmount));

        var periodLines = await (
            from line in db.GLEntryLines.AsNoTracking()
            join entry in db.GLEntries.AsNoTracking() on line.GLEntryId equals entry.Id
            where line.AccountId == request.AccountId
                  && entry.Status == GLEntryStatus.Posted
                  && entry.EntryDate >= request.From && entry.EntryDate <= request.To
            orderby entry.EntryDate, entry.EntryNumber
            select new { entry.Id, entry.EntryDate, entry.EntryNumber, entry.Description, line.DebitAmount, line.CreditAmount }
        ).ToListAsync(cancellationToken);

        var lines = new List<GeneralLedgerLineDto>();
        foreach (var l in periodLines)
        {
            runningBalance += account.ComputeNetBalance(l.DebitAmount, l.CreditAmount);
            lines.Add(new GeneralLedgerLineDto(l.Id, l.EntryDate, l.EntryNumber, l.Description, l.DebitAmount, l.CreditAmount, runningBalance));
        }

        return new GeneralLedgerDto(account.Code, account.Name, request.From, request.To, lines, runningBalance);
    }
}
