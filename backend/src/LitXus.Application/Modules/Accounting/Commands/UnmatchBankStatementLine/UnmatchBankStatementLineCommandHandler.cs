using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.UnmatchBankStatementLine;

public class UnmatchBankStatementLineCommandHandler(IAppDbContext db)
    : IRequestHandler<UnmatchBankStatementLineCommand, BankStatementLineDto>
{
    public async Task<BankStatementLineDto> Handle(UnmatchBankStatementLineCommand request, CancellationToken cancellationToken)
    {
        var statementLine = await db.BankStatementLines.FirstOrDefaultAsync(l => l.Id == request.StatementLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankStatementLine), request.StatementLineId);

        statementLine.Unmatch();
        await db.SaveChangesAsync(cancellationToken);

        return statementLine.ToDto();
    }
}
