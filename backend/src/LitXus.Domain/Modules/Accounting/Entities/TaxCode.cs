using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class TaxCode : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public TaxType Type { get; private set; }

    private TaxCode() { }

    public static TaxCode Create(string code, string name, decimal rate, TaxType type)
    {
        return new TaxCode { Code = code, Name = name, Rate = rate, Type = type };
    }

    /// <summary>2dp, away-from-zero rounding per docs/15_Malaysia_Compliance.md §15.1.</summary>
    public (decimal TaxAmount, decimal Total) Calculate(decimal subTotal)
    {
        var taxAmount = Math.Round(subTotal * (Rate / 100m), 2, MidpointRounding.AwayFromZero);
        return (taxAmount, subTotal + taxAmount);
    }
}
