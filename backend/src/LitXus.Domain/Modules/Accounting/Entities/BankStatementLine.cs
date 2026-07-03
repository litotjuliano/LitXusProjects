using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Exceptions;

namespace LitXus.Domain.Modules.Accounting.Entities;

public class BankStatementLine : BaseEntity
{
    public Guid BankAccountId { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public bool IsReconciled { get; private set; }
    public Guid? MatchedGLEntryLineId { get; private set; }

    private BankStatementLine() { }

    public static BankStatementLine Create(Guid bankAccountId, DateOnly transactionDate, string description, decimal amount)
    {
        return new BankStatementLine
        {
            BankAccountId = bankAccountId,
            TransactionDate = transactionDate,
            Description = description,
            Amount = amount,
        };
    }

    public void Match(Guid glEntryLineId)
    {
        if (IsReconciled)
        {
            throw new StatementLineAlreadyMatchedException();
        }

        MatchedGLEntryLineId = glEntryLineId;
        IsReconciled = true;
    }

    public void Unmatch()
    {
        if (!IsReconciled)
        {
            throw new StatementLineNotMatchedException();
        }

        MatchedGLEntryLineId = null;
        IsReconciled = false;
    }
}
