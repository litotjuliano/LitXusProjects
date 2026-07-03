using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetReconciliationStatus;

public class GetReconciliationStatusQueryHandler(IAppDbContext db) : IRequestHandler<GetReconciliationStatusQuery, ReconciliationStatusDto>
{
    public async Task<ReconciliationStatusDto> Handle(GetReconciliationStatusQuery request, CancellationToken cancellationToken)
    {
        var total = await db.BankStatementLines.CountAsync(l => l.BankAccountId == request.BankAccountId, cancellationToken);
        var matched = await db.BankStatementLines.CountAsync(l => l.BankAccountId == request.BankAccountId && l.IsReconciled, cancellationToken);

        return new ReconciliationStatusDto(total, matched, total - matched);
    }
}
