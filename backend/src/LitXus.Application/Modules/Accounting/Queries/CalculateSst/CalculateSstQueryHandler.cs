using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.CalculateSst;

public class CalculateSstQueryHandler(IAppDbContext db, ISstCalculator sstCalculator)
    : IRequestHandler<CalculateSstQuery, SstCalculationDto>
{
    public async Task<SstCalculationDto> Handle(CalculateSstQuery request, CancellationToken cancellationToken)
    {
        var taxCode = await db.TaxCodes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TaxCodeId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaxCode), request.TaxCodeId);

        var (sstAmount, total) = sstCalculator.Calculate(request.SubTotal, taxCode);
        return new SstCalculationDto(sstAmount, total);
    }
}
