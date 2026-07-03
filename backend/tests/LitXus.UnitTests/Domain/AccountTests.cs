using FluentAssertions;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.UnitTests.Domain;

public class AccountTests
{
    [Theory]
    [InlineData(AccountType.Asset)]
    [InlineData(AccountType.Expense)]
    public void ApplyDebit_OnDebitNormalType_IncreasesBalance(AccountType type)
    {
        var account = Account.Create("1000", "Test", type, null);

        account.ApplyDebit(100m);

        account.Balance.Should().Be(100m);
    }

    [Theory]
    [InlineData(AccountType.Asset)]
    [InlineData(AccountType.Expense)]
    public void ApplyCredit_OnDebitNormalType_DecreasesBalance(AccountType type)
    {
        var account = Account.Create("1000", "Test", type, null);

        account.ApplyCredit(100m);

        account.Balance.Should().Be(-100m);
    }

    [Theory]
    [InlineData(AccountType.Liability)]
    [InlineData(AccountType.Equity)]
    [InlineData(AccountType.Revenue)]
    public void ApplyCredit_OnCreditNormalType_IncreasesBalance(AccountType type)
    {
        var account = Account.Create("2000", "Test", type, null);

        account.ApplyCredit(100m);

        account.Balance.Should().Be(100m);
    }

    [Theory]
    [InlineData(AccountType.Liability)]
    [InlineData(AccountType.Equity)]
    [InlineData(AccountType.Revenue)]
    public void ApplyDebit_OnCreditNormalType_DecreasesBalance(AccountType type)
    {
        var account = Account.Create("2000", "Test", type, null);

        account.ApplyDebit(100m);

        account.Balance.Should().Be(-100m);
    }

    [Fact]
    public void Rename_ChangesNameAndParent_LeavesCodeUnchanged()
    {
        var parentId = Guid.NewGuid();
        var account = Account.Create("1010", "Original Name", AccountType.Asset, null);

        account.Rename("New Name", parentId);

        account.Name.Should().Be("New Name");
        account.ParentAccountId.Should().Be(parentId);
        account.Code.Should().Be("1010");
    }

    [Fact]
    public void SetActive_TogglesIsActive()
    {
        var account = Account.Create("1010", "Cash", AccountType.Asset, null);
        account.IsActive.Should().BeTrue();

        account.SetActive(false);
        account.IsActive.Should().BeFalse();

        account.SetActive(true);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ComputeNetBalance_ForDebitNormalAccount_IsDebitMinusCredit()
    {
        var account = Account.Create("1010", "Cash", AccountType.Asset, null);

        account.ComputeNetBalance(totalDebit: 300m, totalCredit: 100m).Should().Be(200m);
    }

    [Fact]
    public void ComputeNetBalance_ForCreditNormalAccount_IsCreditMinusDebit()
    {
        var account = Account.Create("4010", "Revenue", AccountType.Revenue, null);

        account.ComputeNetBalance(totalDebit: 100m, totalCredit: 300m).Should().Be(200m);
    }

    [Fact]
    public void GetTrialBalanceColumns_ForDebitNormalAccount_WithNormalBalance_ShowsOnDebitSide()
    {
        var account = Account.Create("1010", "Cash", AccountType.Asset, null);

        var (debit, credit) = account.GetTrialBalanceColumns(totalDebit: 500m, totalCredit: 200m);

        debit.Should().Be(300m);
        credit.Should().Be(0m);
    }

    [Fact]
    public void GetTrialBalanceColumns_ForDebitNormalAccount_WithAbnormalBalance_ShowsOnCreditSide()
    {
        // A debit-normal account that's gone credit (e.g. an overdrawn contra-asset) should
        // present as a positive amount on the opposite side, not a negative number.
        var account = Account.Create("1010", "Cash", AccountType.Asset, null);

        var (debit, credit) = account.GetTrialBalanceColumns(totalDebit: 100m, totalCredit: 400m);

        debit.Should().Be(0m);
        credit.Should().Be(300m);
    }

    [Fact]
    public void GetTrialBalanceColumns_ForCreditNormalAccount_WithNormalBalance_ShowsOnCreditSide()
    {
        var account = Account.Create("4010", "Revenue", AccountType.Revenue, null);

        var (debit, credit) = account.GetTrialBalanceColumns(totalDebit: 100m, totalCredit: 500m);

        debit.Should().Be(0m);
        credit.Should().Be(400m);
    }
}
