using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetBankAccounts;

public class GetBankAccountsQueryHandler(IAppDbContext db) : IRequestHandler<GetBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var results = await (
            from bankAccount in db.BankAccounts.AsNoTracking()
            join account in db.Accounts.AsNoTracking() on bankAccount.AccountId equals account.Id
            orderby bankAccount.BankName
            select new BankAccountDto(
                bankAccount.Id, account.Id, account.Code, account.Name,
                bankAccount.BankName, bankAccount.AccountNumber, bankAccount.Currency)
        ).ToListAsync(cancellationToken);

        return results;
    }
}
