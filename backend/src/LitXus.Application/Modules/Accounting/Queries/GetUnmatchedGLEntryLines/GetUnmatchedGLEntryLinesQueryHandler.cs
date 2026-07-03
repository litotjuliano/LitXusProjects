using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetUnmatchedGLEntryLines;

public class GetUnmatchedGLEntryLinesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUnmatchedGLEntryLinesQuery, IReadOnlyList<UnmatchedGLEntryLineDto>>
{
    public async Task<IReadOnlyList<UnmatchedGLEntryLineDto>> Handle(GetUnmatchedGLEntryLinesQuery request, CancellationToken cancellationToken)
    {
        var bankAccount = await db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BankAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankAccount), request.BankAccountId);

        var matchedIds = db.BankStatementLines.AsNoTracking()
            .Where(l => l.MatchedGLEntryLineId != null)
            .Select(l => l.MatchedGLEntryLineId!.Value);

        var results = await (
            from line in db.GLEntryLines.AsNoTracking()
            join entry in db.GLEntries.AsNoTracking() on line.GLEntryId equals entry.Id
            where line.AccountId == bankAccount.AccountId
                  && entry.Status == GLEntryStatus.Posted
                  && !matchedIds.Contains(line.Id)
            orderby entry.EntryDate descending
            select new UnmatchedGLEntryLineDto(line.Id, entry.Id, entry.EntryDate, entry.EntryNumber, entry.Description, line.DebitAmount, line.CreditAmount)
        ).ToListAsync(cancellationToken);

        return results;
    }
}
