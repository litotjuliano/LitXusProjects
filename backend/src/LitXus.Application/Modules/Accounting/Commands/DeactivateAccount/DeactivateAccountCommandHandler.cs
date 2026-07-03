using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Account = LitXus.Domain.Modules.Accounting.Entities.Account;

namespace LitXus.Application.Modules.Accounting.Commands.DeactivateAccount;

public class DeactivateAccountCommandHandler(IAppDbContext db) : IRequestHandler<DeactivateAccountCommand>
{
    public async Task Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        account.SetActive(false);
        await db.SaveChangesAsync(cancellationToken);
    }
}
