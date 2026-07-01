using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class BankAccount : BaseEntity
{
    public Guid AccountId { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "MYR";

    private BankAccount() { }

    public static BankAccount Create(Guid accountId, string bankName, string accountNumber)
    {
        return new BankAccount { AccountId = accountId, BankName = bankName, AccountNumber = accountNumber };
    }
}
