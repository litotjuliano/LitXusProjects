using FluentAssertions;
using LitXus.Application.Modules.Accounting.Services;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.UnitTests.Application;

public class SstCalculatorTests
{
    [Fact]
    public void Calculate_DelegatesToTaxCodeCalculate_SameResult()
    {
        var taxCode = TaxCode.Create("SST-6", "Sales & Service Tax 6%", 6.00m, TaxType.Sst);
        var calculator = new SstCalculator();

        var (sstAmount, total) = calculator.Calculate(1000m, taxCode);
        var expected = taxCode.Calculate(1000m);

        sstAmount.Should().Be(expected.TaxAmount);
        total.Should().Be(expected.Total);
        sstAmount.Should().Be(60.00m);
        total.Should().Be(1060.00m);
    }

    [Fact]
    public void Calculate_AtZeroRate_ReturnsZeroSst()
    {
        var taxCode = TaxCode.Create("SST-0", "Zero-rated", 0.00m, TaxType.Sst);
        var calculator = new SstCalculator();

        var (sstAmount, total) = calculator.Calculate(500m, taxCode);

        sstAmount.Should().Be(0m);
        total.Should().Be(500m);
    }
}
