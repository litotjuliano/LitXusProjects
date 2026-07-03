using FluentAssertions;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Accounting.Exceptions;

namespace LitXus.UnitTests.Domain;

public class GLEntryTests
{
    private static Account NewAccount(AccountType type, string code = "1000") =>
        Account.Create(code, $"{type} Account", type, null);

    private static GLEntry BalancedDraft(Account debitAccount, Account creditAccount, decimal amount = 100m)
    {
        var lines = new[]
        {
            GLEntryLine.Create(debitAccount, amount, 0m, null),
            GLEntryLine.Create(creditAccount, 0m, amount, null),
        };
        return GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "Test entry", lines);
    }

    [Fact]
    public void CreateDraft_StartsInDraftStatusWithGivenLines()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue);

        entry.Status.Should().Be(GLEntryStatus.Draft);
        entry.Lines.Should().HaveCount(2);
        entry.EntryNumber.Should().BeNull();
    }

    [Fact]
    public void Post_WhenBalanced_TransitionsToPostedAndUpdatesAccountBalances()
    {
        var cash = NewAccount(AccountType.Asset, "1010"); // debit-normal
        var revenue = NewAccount(AccountType.Revenue, "4010"); // credit-normal
        var entry = BalancedDraft(cash, revenue, 250m);

        entry.Post("JE-2026-000001", Guid.NewGuid(), DateTime.UtcNow);

        entry.Status.Should().Be(GLEntryStatus.Posted);
        entry.EntryNumber.Should().Be("JE-2026-000001");
        entry.PostedAtUtc.Should().NotBeNull();
        cash.Balance.Should().Be(250m); // debit increases a debit-normal account
        revenue.Balance.Should().Be(250m); // credit increases a credit-normal account
    }

    [Fact]
    public void Post_WhenUnbalanced_ThrowsEntryUnbalancedException_WithExactMessage()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var lines = new[]
        {
            GLEntryLine.Create(cash, 100m, 0m, null),
            GLEntryLine.Create(revenue, 0m, 90m, null),
        };
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "Unbalanced", lines);

        var act = () => entry.Post("JE-2026-000002", Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<EntryUnbalancedException>()
            .WithMessage("Entry is unbalanced by RM 10.00 (debit exceeds the other side).");
        entry.Status.Should().Be(GLEntryStatus.Draft);
    }

    [Fact]
    public void Post_WhenFewerThanTwoLines_ThrowsEntryTooFewLinesException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var lines = new[] { GLEntryLine.Create(cash, 100m, 0m, null) };
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "Single line", lines);

        var act = () => entry.Post("JE-2026-000003", Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<EntryTooFewLinesException>();
    }

    [Fact]
    public void Post_WhenAlreadyPosted_ThrowsEntryNotDraftException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue);
        entry.Post("JE-2026-000004", Guid.NewGuid(), DateTime.UtcNow);

        var act = () => entry.Post("JE-2026-000005", Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<EntryNotDraftException>();
    }

    [Fact]
    public void Post_WhenLineAccountIsInactive_ThrowsAccountInactiveException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        revenue.SetActive(false);
        var entry = BalancedDraft(cash, revenue);

        var act = () => entry.Post("JE-2026-000006", Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<AccountInactiveException>();
    }

    [Fact]
    public void Post_AllowsZeroValueBalancedEntry()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue, 0m);

        var act = () => entry.Post("JE-2026-000007", Guid.NewGuid(), DateTime.UtcNow);

        act.Should().NotThrow();
        entry.Status.Should().Be(GLEntryStatus.Posted);
    }

    [Fact]
    public void Void_WhenPosted_TransitionsToVoidedAndReversesBalances()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue, 100m);
        entry.Post("JE-2026-000008", Guid.NewGuid(), DateTime.UtcNow);

        entry.Void("Duplicate entry");

        entry.Status.Should().Be(GLEntryStatus.Voided);
        entry.VoidReason.Should().Be("Duplicate entry");
        entry.EntryNumber.Should().Be("JE-2026-000008"); // never reused
        cash.Balance.Should().Be(0m);
        revenue.Balance.Should().Be(0m);
    }

    [Fact]
    public void Void_WhenReasonIsEmpty_ThrowsVoidRequiresReasonException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue);
        entry.Post("JE-2026-000009", Guid.NewGuid(), DateTime.UtcNow);

        var act = () => entry.Void("   ");

        act.Should().Throw<VoidRequiresReasonException>();
    }

    [Fact]
    public void Void_WhenEntryIsStillDraft_ThrowsEntryNotDraftException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue);

        var act = () => entry.Void("reason");

        act.Should().Throw<EntryNotDraftException>();
    }

    [Fact]
    public void UpdateLines_WhenDraft_ReplacesDateDescriptionAndLines()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue, 100m);

        var newLines = new[]
        {
            GLEntryLine.Create(cash, 200m, 0m, null),
            GLEntryLine.Create(revenue, 0m, 200m, null),
        };
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        entry.UpdateLines(newDate, "Updated description", newLines);

        entry.EntryDate.Should().Be(newDate);
        entry.Description.Should().Be("Updated description");
        entry.Lines.Should().HaveCount(2);
        entry.Lines.Sum(l => l.DebitAmount).Should().Be(200m);
    }

    [Fact]
    public void UpdateLines_WhenNotDraft_ThrowsEntryNotDraftException()
    {
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        var entry = BalancedDraft(cash, revenue);
        entry.Post("JE-2026-000010", Guid.NewGuid(), DateTime.UtcNow);

        var act = () => entry.UpdateLines(entry.EntryDate, "New description", entry.Lines.ToList());

        act.Should().Throw<EntryNotDraftException>();
    }
}
