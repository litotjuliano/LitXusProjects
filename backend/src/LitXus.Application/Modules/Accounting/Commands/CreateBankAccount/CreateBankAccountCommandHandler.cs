using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Account = LitXus.Domain.Modules.Accounting.Entities.Account;

namespace LitXus.Application.Modules.Accounting.Commands.CreateBankAccount;

public class CreateBankAccountCommandHandler(IAppDbContext db) : IRequestHandler<CreateBankAccountCommand, BankAccountDto>
{
    public async Task<BankAccountDto> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var bankAccount = BankAccount.Create(request.AccountId, request.BankName, request.AccountNumber);

        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(cancellationToken);

        return new BankAccountDto(bankAccount.Id, account.Id, account.Code, account.Name, bankAccount.BankName, bankAccount.AccountNumber, bankAccount.Currency);
    }
}
