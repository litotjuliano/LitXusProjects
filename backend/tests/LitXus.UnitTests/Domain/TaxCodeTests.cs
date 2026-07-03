using FluentAssertions;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.UnitTests.Domain;

public class TaxCodeTests
{
    [Fact]
    public void Calculate_AtSixPercent_ReturnsCorrectTaxAndTotal()
    {
        var sst = TaxCode.Create("SST-6", "Sales & Service Tax 6%", 6.00m, TaxType.Sst);

        var (taxAmount, total) = sst.Calculate(1000m);

        taxAmount.Should().Be(60.00m);
        total.Should().Be(1060.00m);
    }

    [Fact]
    public void Calculate_AtZeroPercent_ReturnsZeroTax()
    {
        var exempt = TaxCode.Create("SST-0", "Zero-rated", 0.00m, TaxType.Sst);

        var (taxAmount, total) = exempt.Calculate(500m);

        taxAmount.Should().Be(0m);
        total.Should().Be(500m);
    }

    [Fact]
    public void Calculate_RoundsHalfAwayFromZero_NotToEven()
    {
        // subTotal * rate lands exactly on a half-cent (2.5 -> should round to 0.03, not 0.02 as
        // banker's rounding/MidpointRounding.ToEven would produce) — the exact rule Malaysia
        // compliance requires per docs/15_Malaysia_Compliance.md §15.1.
        var tax = TaxCode.Create("TEST-1", "Test 1%", 1m, TaxType.Sst);

        var (taxAmount, _) = tax.Calculate(2.50m);

        taxAmount.Should().Be(0.03m);
    }
}
