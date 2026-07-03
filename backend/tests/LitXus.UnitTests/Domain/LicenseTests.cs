using FluentAssertions;
using LitXus.Domain.Modules.Shared.Entities;

namespace LitXus.UnitTests.Domain;

public class LicenseTests
{
    [Fact]
    public void ApplyVerifiedKey_SetsAllFieldsAtomicallyFromTokenClaims()
    {
        var license = License.Create(
            "OldProduct", "Accounting", "Old Company Sdn Bhd",
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1), "old-token");

        var issuedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiresAt = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        license.ApplyVerifiedKey(
            "AccountingPro", "Acme Sdn Bhd", ["Accounting", "Sales"], issuedAt, expiresAt, "new-signed-token");

        license.ProductCode.Should().Be("AccountingPro");
        license.IssuedToCompany.Should().Be("Acme Sdn Bhd");
        license.EnabledModules.Should().Be("Accounting,Sales");
        license.IssuedAtUtc.Should().Be(issuedAt);
        license.ExpiresAtUtc.Should().Be(expiresAt);
        license.LicenseKey.Should().Be("new-signed-token");
    }

    [Fact]
    public void GetEnabledModuleList_SplitsCommaSeparatedModules()
    {
        var license = License.Create(
            "AccountingPro", "Accounting,Sales,Inventory", "Acme Sdn Bhd",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "token");

        license.GetEnabledModuleList().Should().BeEquivalentTo(["Accounting", "Sales", "Inventory"]);
    }

    [Fact]
    public void IsExpired_WhenPastExpiryDate_ReturnsTrue()
    {
        var license = License.Create(
            "AccountingPro", "Accounting", "Acme Sdn Bhd",
            DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddDays(-1), "token");

        license.IsExpired(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenBeforeExpiryDate_ReturnsFalse()
    {
        var license = License.Create(
            "AccountingPro", "Accounting", "Acme Sdn Bhd",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "token");

        license.IsExpired(DateTime.UtcNow).Should().BeFalse();
    }
}
