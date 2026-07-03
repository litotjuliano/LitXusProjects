using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetBankStatementLines;

public class GetBankStatementLinesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetBankStatementLinesQuery, IReadOnlyList<BankStatementLineDto>>
{
    public async Task<IReadOnlyList<BankStatementLineDto>> Handle(GetBankStatementLinesQuery request, CancellationToken cancellationToken)
    {
        var lines = await db.BankStatementLines.AsNoTracking()
            .Where(l => l.BankAccountId == request.BankAccountId)
            .OrderByDescending(l => l.TransactionDate)
            .ToListAsync(cancellationToken);

        return lines.Select(l => l.ToDto()).ToList();
    }
}
