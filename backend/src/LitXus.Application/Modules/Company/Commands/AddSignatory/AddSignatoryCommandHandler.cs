using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = LitXus.Domain.Modules.Shared.Entities.Company;
using CompanySignatoryEntity = LitXus.Domain.Modules.Shared.Entities.CompanySignatory;

namespace LitXus.Application.Modules.Company.Commands.AddSignatory;

public class AddSignatoryCommandHandler(IAppDbContext db) : IRequestHandler<AddSignatoryCommand, CompanySignatoryDto>
{
    public async Task<CompanySignatoryDto> Handle(AddSignatoryCommand request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyEntity), "profile");

        var signatory = CompanySignatoryEntity.Create(company.Id, request.Name, request.IcNumber, request.Position, request.Email, request.Phone);
        db.CompanySignatories.Add(signatory);
        await db.SaveChangesAsync(cancellationToken);

        return signatory.ToDto();
    }
}
