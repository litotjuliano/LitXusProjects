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

    private bool IsDebitNormal => Type is AccountType.Asset or AccountType.Expense;
}
