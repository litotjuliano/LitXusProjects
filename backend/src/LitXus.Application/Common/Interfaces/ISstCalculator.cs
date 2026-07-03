using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Common.Interfaces;

/// <summary>
/// Single source of truth for SST calculation, called from both the standalone
/// /tax/calculate-sst endpoint and (in later phases) Sales invoice line calculation —
/// see docs/15_Malaysia_Compliance.md §15.1.
/// </summary>
public interface ISstCalculator
{
    (decimal SstAmount, decimal Total) Calculate(decimal subTotal, TaxCode taxCode);
}
