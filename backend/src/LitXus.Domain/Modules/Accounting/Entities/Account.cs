using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class Account : BaseEntity, IAuditable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public Guid? ParentAccountId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal Balance { get; private set; }

    private Account() { }

    public static Account Create(string code, string name, AccountType type, Guid? parentAccountId)
    {
        return new Account
        {
            Code = code,
            Name = name,
            Type = type,
            ParentAccountId = parentAccountId,
        };
    }

    /// <summary>Code is immutable once set (docs/phase-1-accounting/Business_Rules.md — "Account codes are unique and immutable after creation").</summary>
    public void Rename(string name, Guid? parentAccountId)
    {
        Name = name;
        ParentAccountId = parentAccountId;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void ApplyDebit(decimal amount) => Balance += IsDebitNormal ? amount : -amount;

    public void ApplyCredit(decimal amount) => Balance += IsDebitNormal ? -amount : amount;

    /// <summary>
    /// Net balance from raw debit/credit totals, honoring this account's normal balance side —
    /// used by reports that compute a point-in-time balance from GLEntryLines directly rather
    /// than reading the (always-current) Balance field. Kept here, not duplicated in Application,
    /// since which accounts are debit-normal vs. credit-normal is an accounting rule, not a
    /// reporting concern.
    /// </summary>
    public decimal ComputeNetBalance(decimal totalDebit, decimal totalCredit) =>
        IsDebitNormal ? totalDebit - totalCredit : totalCredit - totalDebit;

    /// <summary>
    /// Debit/credit columns for a two-column trial balance presentation: a normal-balance
    /// account's net position shows on its own side; an abnormal (e.g. contra-account) balance
    /// shows as a positive amount on the opposite side, rather than a negative number.
    /// </summary>
    public (decimal Debit, decimal Credit) GetTrialBalanceColumns(decimal totalDebit, decimal totalCredit)
    {
        var net = ComputeNetBalance(totalDebit, totalCredit);
        if (IsDebitNormal)
        {
            return net >= 0 ? (net, 0m) : (0m, -net);
        }
        return net >= 0 ? (0m, net) : (-net, 0m);
    }

    private bool IsDebitNormal => Type is AccountType.Asset or AccountType.Expense;
}
