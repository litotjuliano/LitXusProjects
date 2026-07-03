using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Modules.Accounting.Dtos;

public record BankAccountDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string BankName,
    string AccountNumber,
    string Currency);

public record BankStatementLineDto(
    Guid Id,
    Guid BankAccountId,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    bool IsReconciled,
    Guid? MatchedGLEntryLineId);

public record UnmatchedGLEntryLineDto(
    Guid GLEntryLineId,
    Guid GLEntryId,
    DateOnly EntryDate,
    string? EntryNumber,
    string Description,
    decimal DebitAmount,
    decimal CreditAmount);

public record ReconciliationStatusDto(int TotalStatementLines, int MatchedStatementLines, int UnmatchedStatementLines);

public static class BankAccountMappingExtensions
{
    public static BankStatementLineDto ToDto(this BankStatementLine line) =>
        new(line.Id, line.BankAccountId, line.TransactionDate, line.Description, line.Amount, line.IsReconciled, line.MatchedGLEntryLineId);
}
