using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetAccounts;

public class GetAccountsQueryHandler(IAppDbContext db) : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Accounts.AsNoTracking().AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<AccountType>(request.Type, out var type))
        {
            query = query.Where(a => a.Type == type);
        }

        var accounts = await query.OrderBy(a => a.Code).ToListAsync(cancellationToken);
        return accounts.Select(a => a.ToDto()).ToList();
    }
}
