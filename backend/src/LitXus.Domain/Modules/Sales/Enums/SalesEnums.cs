namespace LitXus.Domain.Modules.Sales.Enums;

public enum InvoiceStatus
{
    Draft,
    Issued,
    PartiallyPaid,
    Paid,
    Void,
    // Overdue is deliberately not a stored value — it's computed at query time from
    // DueDate/Status (see Invoice.IsOverdue), not a transition anything sets explicitly.
}

public enum PaymentMethod
{
    BankTransfer,
    Cash,
    Cheque,
    OnlineGateway,
}

public enum PaymentStatus
{
    Pending,
    Verified,
    Rejected,
}

public enum CreditNoteStatus
{
    Draft,
    Issued,
    Applied,
}
