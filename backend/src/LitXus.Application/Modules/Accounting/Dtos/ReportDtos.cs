namespace LitXus.Application.Modules.Accounting.Dtos;

public record TrialBalanceLineDto(string AccountCode, string AccountName, string AccountType, decimal Debit, decimal Credit);

public record TrialBalanceDto(
    DateOnly AsOfDate,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit);

public record IncomeStatementLineDto(string AccountCode, string AccountName, decimal Amount);

public record IncomeStatementDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<IncomeStatementLineDto> Revenue,
    IReadOnlyList<IncomeStatementLineDto> Expenses,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetIncome);

public record BalanceSheetLineDto(string AccountCode, string AccountName, decimal Balance);

public record BalanceSheetDto(
    DateOnly AsOfDate,
    IReadOnlyList<BalanceSheetLineDto> Assets,
    IReadOnlyList<BalanceSheetLineDto> Liabilities,
    IReadOnlyList<BalanceSheetLineDto> Equity,
    decimal CurrentYearEarnings,
    decimal TotalAssets,
    decimal TotalLiabilitiesAndEquity);

public record GeneralLedgerLineDto(
    Guid GLEntryId,
    DateOnly EntryDate,
    string? EntryNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

public record GeneralLedgerDto(
    string AccountCode,
    string AccountName,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<GeneralLedgerLineDto> Lines,
    decimal EndingBalance);
