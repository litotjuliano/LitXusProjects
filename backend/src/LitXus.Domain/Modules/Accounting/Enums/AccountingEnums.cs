namespace LitXus.Domain.Modules.Accounting.Enums;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
}

public enum GLEntryStatus
{
    Draft,
    Posted,
    Voided,
}

public enum GLEntrySource
{
    Manual,
    SalesAutoPost,
    InventoryAutoPost,
}

public enum TaxType
{
    Sst,
    IncomeTax,
}
