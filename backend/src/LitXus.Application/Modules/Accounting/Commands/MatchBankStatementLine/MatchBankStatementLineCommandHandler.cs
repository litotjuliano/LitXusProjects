using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Accounting.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.MatchBankStatementLine;

public class MatchBankStatementLineCommandHandler(IAppDbContext db)
    : IRequestHandler<MatchBankStatementLineCommand, BankStatementLineDto>
{
    public async Task<BankStatementLineDto> Handle(MatchBankStatementLineCommand request, CancellationToken cancellationToken)
    {
        var statementLine = await db.BankStatementLines.FirstOrDefaultAsync(l => l.Id == request.StatementLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankStatementLine), request.StatementLineId);

        var glEntryLine = await db.GLEntryLines.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.GLEntryLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(GLEntryLine), request.GLEntryLineId);

        var glEntry = await db.GLEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == glEntryLine.GLEntryId, cancellationToken);
        if (glEntry is null || glEntry.Status != GLEntryStatus.Posted)
        {
            // Only Posted lines are reconciliation-eligible — the same NotFoundException a caller
            // would get for a nonexistent line, since the "unmatched GL lines" list the UI matches
            // against is already pre-filtered to Posted, so this only fires on a stale/invalid id.
            throw new NotFoundException(nameof(GLEntryLine), request.GLEntryLineId);
        }

        var alreadyMatchedElsewhere = await db.BankStatementLines.AnyAsync(
            l => l.MatchedGLEntryLineId == request.GLEntryLineId && l.Id != request.StatementLineId, cancellationToken);
        if (alreadyMatchedElsewhere)
        {
            throw new GLEntryLineAlreadyMatchedException();
        }

        statementLine.Match(request.GLEntryLineId);
        await db.SaveChangesAsync(cancellationToken);

        return statementLine.ToDto();
    }
}
