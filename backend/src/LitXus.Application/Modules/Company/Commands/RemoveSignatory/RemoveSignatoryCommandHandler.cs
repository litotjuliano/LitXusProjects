using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanySignatoryEntity = LitXus.Domain.Modules.Shared.Entities.CompanySignatory;

namespace LitXus.Application.Modules.Company.Commands.RemoveSignatory;

public class RemoveSignatoryCommandHandler(IAppDbContext db) : IRequestHandler<RemoveSignatoryCommand>
{
    public async Task Handle(RemoveSignatoryCommand request, CancellationToken cancellationToken)
    {
        var signatory = await db.CompanySignatories.FirstOrDefaultAsync(s => s.Id == request.SignatoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanySignatoryEntity), request.SignatoryId);

        db.CompanySignatories.Remove(signatory);
        await db.SaveChangesAsync(cancellationToken);
    }
}
