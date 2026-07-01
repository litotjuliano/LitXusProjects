using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Accounting.Exceptions;

public sealed class EntryNotDraftException()
    : DomainException("ENTRY_NOT_DRAFT", "Only entries in Draft status can be edited, posted, or voided.");

public sealed class EntryUnbalancedException(decimal totalDebit, decimal totalCredit)
    : DomainException("ENTRY_UNBALANCED", BuildMessage(totalDebit, totalCredit))
{
    private static string BuildMessage(decimal totalDebit, decimal totalCredit)
    {
        var delta = Math.Abs(totalDebit - totalCredit);
        var side = totalDebit > totalCredit ? "debit" : "credit";
        return $"Entry is unbalanced by RM {delta:N2} ({side} exceeds the other side).";
    }
}

public sealed class EntryTooFewLinesException()
    : DomainException("ENTRY_TOO_FEW_LINES", "A GL entry needs at least 2 lines to be posted.");

public sealed class AccountCodeDuplicateException(string code)
    : DomainException("ACCOUNT_CODE_DUPLICATE", $"An account with code '{code}' already exists.");

public sealed class AccountInactiveException(string code)
    : DomainException("ACCOUNT_INACTIVE", $"Account '{code}' is inactive and cannot be used on new GL entry lines.");

public sealed class VoidRequiresReasonException()
    : DomainException("VOID_REQUIRES_REASON", "A reason is required to void a GL entry.");

public sealed class StatementLineAlreadyMatchedException()
    : DomainException("STATEMENT_LINE_ALREADY_MATCHED", "This bank statement line is already reconciled.");
