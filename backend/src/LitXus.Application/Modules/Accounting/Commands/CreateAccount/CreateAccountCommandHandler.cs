using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Accounting.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Account = LitXus.Domain.Modules.Accounting.Entities.Account;

namespace LitXus.Application.Modules.Accounting.Commands.CreateAccount;

public class CreateAccountCommandHandler(IAppDbContext db) : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await db.Accounts.AnyAsync(a => a.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new AccountCodeDuplicateException(request.Code);
        }

        var account = Account.Create(
            request.Code,
            request.Name,
            Enum.Parse<AccountType>(request.Type),
            request.ParentAccountId);

        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        return account.ToDto();
    }
}
