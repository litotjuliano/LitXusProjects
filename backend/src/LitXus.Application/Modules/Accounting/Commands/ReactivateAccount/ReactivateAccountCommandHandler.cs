using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Account = LitXus.Domain.Modules.Accounting.Entities.Account;

namespace LitXus.Application.Modules.Accounting.Commands.ReactivateAccount;

public class ReactivateAccountCommandHandler(IAppDbContext db) : IRequestHandler<ReactivateAccountCommand>
{
    public async Task Handle(ReactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        account.SetActive(true);
        await db.SaveChangesAsync(cancellationToken);
    }
}
