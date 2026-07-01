using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class GLEntryLine : BaseEntity
{
    public Guid GLEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public string? LineDescription { get; private set; }

    private GLEntryLine() { }

    public static GLEntryLine Create(Account account, decimal debitAmount, decimal creditAmount, string? lineDescription)
    {
        return new GLEntryLine
        {
            AccountId = account.Id,
            Account = account,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            LineDescription = lineDescription,
        };
    }

    internal void AttachTo(Guid glEntryId) => GLEntryId = glEntryId;
}
