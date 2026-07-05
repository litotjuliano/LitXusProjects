using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Entities;

/// <summary>
/// One row per install — the GL account mapping Sales auto-posting needs, since Phase 1's Chart
/// of Accounts is fully user-defined (nothing says which account is "the" receivable/revenue/tax
/// account until an Admin says so). Nullable until configured — GL-posting handlers throw
/// SalesSettingsNotConfiguredException if an invoice is issued before setup.
/// </summary>
public class SalesSettings : BaseEntity
{
    public Guid? DefaultReceivableAccountId { get; private set; }
    public Guid? DefaultRevenueAccountId { get; private set; }
    public Guid? DefaultTaxPayableAccountId { get; private set; }
    public Guid? DefaultCashAccountId { get; private set; }

    private SalesSettings() { }

    public static SalesSettings CreateEmpty() => new();

    public void Configure(Guid? receivableAccountId, Guid? revenueAccountId, Guid? taxPayableAccountId, Guid? cashAccountId)
    {
        DefaultReceivableAccountId = receivableAccountId;
        DefaultRevenueAccountId = revenueAccountId;
        DefaultTaxPayableAccountId = taxPayableAccountId;
        DefaultCashAccountId = cashAccountId;
    }

    public bool IsConfigured =>
        DefaultReceivableAccountId.HasValue && DefaultRevenueAccountId.HasValue &&
        DefaultTaxPayableAccountId.HasValue && DefaultCashAccountId.HasValue;
}
