using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Accounting.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaxCode = LitXus.Domain.Modules.Accounting.Entities.TaxCode;

namespace LitXus.Application.Modules.Accounting.Commands.CreateTaxCode;

public class CreateTaxCodeCommandHandler(IAppDbContext db) : IRequestHandler<CreateTaxCodeCommand, TaxCodeDto>
{
    public async Task<TaxCodeDto> Handle(CreateTaxCodeCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await db.TaxCodes.AnyAsync(t => t.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new TaxCodeDuplicateException(request.Code);
        }

        var taxCode = TaxCode.Create(request.Code, request.Name, request.Rate, Enum.Parse<TaxType>(request.Type));

        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync(cancellationToken);

        return taxCode.ToDto();
    }
}
