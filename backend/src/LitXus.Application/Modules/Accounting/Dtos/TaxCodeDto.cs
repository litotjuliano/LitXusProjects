using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Modules.Accounting.Dtos;

public record TaxCodeDto(Guid Id, string Code, string Name, decimal Rate, string Type);

public record SstCalculationDto(decimal SstAmount, decimal Total);

public static class TaxCodeMappingExtensions
{
    public static TaxCodeDto ToDto(this TaxCode taxCode) =>
        new(taxCode.Id, taxCode.Code, taxCode.Name, taxCode.Rate, taxCode.Type.ToString());
}
