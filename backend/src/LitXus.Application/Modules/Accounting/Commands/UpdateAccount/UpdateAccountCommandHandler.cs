using FluentValidation.Results;
using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Account = LitXus.Domain.Modules.Accounting.Entities.Account;

namespace LitXus.Application.Modules.Accounting.Commands.UpdateAccount;

public class UpdateAccountCommandHandler(IAppDbContext db) : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        if (request.ParentAccountId is { } parentId)
        {
            if (parentId == request.Id)
            {
                throw new ValidationException([new ValidationFailure("parentAccountId", "An account cannot be its own parent.")]);
            }

            // Walk the requested parent's ancestor chain — if it passes back through this account,
            // reparenting would create a cycle in the parent/child tree.
            var allAccounts = await db.Accounts.Select(a => new { a.Id, a.ParentAccountId }).ToListAsync(cancellationToken);
            var byId = allAccounts.ToDictionary(a => a.Id, a => a.ParentAccountId);
            if (!byId.ContainsKey(parentId))
            {
                throw new NotFoundException(nameof(Account), parentId);
            }

            var cursor = byId[parentId];
            while (cursor is not null)
            {
                if (cursor == request.Id)
                {
                    throw new ValidationException([new ValidationFailure("parentAccountId", "This would create a circular parent/child relationship.")]);
                }
                cursor = byId.TryGetValue(cursor.Value, out var next) ? next : null;
            }
        }

        account.Rename(request.Name, request.ParentAccountId);
        await db.SaveChangesAsync(cancellationToken);

        return account.ToDto();
    }
}
