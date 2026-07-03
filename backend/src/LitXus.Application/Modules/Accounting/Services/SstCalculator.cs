using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Modules.Accounting.Services;

/// <summary>Delegates to TaxCode.Calculate() — the domain entity already owns the 2dp
/// away-from-zero rounding rule (docs/15_Malaysia_Compliance.md §15.1); this service exists
/// so callers depend on an interface, not the entity, per the documented architecture.</summary>
public class SstCalculator : ISstCalculator
{
    public (decimal SstAmount, decimal Total) Calculate(decimal subTotal, TaxCode taxCode) => taxCode.Calculate(subTotal);
}
