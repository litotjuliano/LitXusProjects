using LitXus.Domain.Common;
using LitXus.Domain.Modules.Sales.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;

namespace LitXus.Domain.Modules.Sales.Entities;

public class Invoice : BaseEntity, IAuditable
{
    private readonly List<InvoiceLine> _lines = [];

    public string? InvoiceNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal SSTAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public string? Notes { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    /// <summary>Computed, not a stored transition — no scheduled-job infrastructure exists to
    /// flip this automatically, and a computed value is always correct without one.</summary>
    public bool IsOverdue(DateOnly today) =>
        Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && DueDate < today;

    public decimal OutstandingBalance => TotalAmount - AmountPaid;

    private Invoice() { }

    public static Invoice CreateDraft(Guid customerId, DateOnly invoiceDate, DateOnly dueDate, string? notes, IEnumerable<InvoiceLine> lines)
    {
        var invoice = new Invoice
        {
            CustomerId = customerId,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            Notes = notes,
        };
        invoice.ReplaceLines(lines);
        return invoice;
    }

    /// <summary>Only Draft invoices are editable — matches GLEntry.UpdateLines' convention.</summary>
    public void UpdateLines(DateOnly invoiceDate, DateOnly dueDate, string? notes, IEnumerable<InvoiceLine> lines)
    {
        EnsureIsDraft();
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        Notes = notes;
        ReplaceLines(lines);
    }

    private void ReplaceLines(IEnumerable<InvoiceLine> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
        {
            line.AttachTo(Id);
            _lines.Add(line);
        }

        SubTotal = _lines.Sum(l => l.LineTotal);
        SSTAmount = _lines.Sum(l => l.ComputeTaxAmount());
        TotalAmount = SubTotal + SSTAmount;
    }

    /// <summary>Assigns the sequential invoice number and transitions Draft -> Issued, mirroring
    /// GLEntry.Post's "number assigned at transition, not creation" pattern (a Draft invoice may
    /// never be issued). Raises InvoiceIssuedEvent — see docs/01_Architecture.md §1.4.</summary>
    public void Issue(string invoiceNumber)
    {
        EnsureIsDraft();

        if (_lines.Count < 1)
        {
            throw new InvoiceTooFewLinesException();
        }

        InvoiceNumber = invoiceNumber;
        Status = InvoiceStatus.Issued;
        AddDomainEvent(new InvoiceIssuedEvent(Id));
    }

    /// <summary>Called only when a payment is Verified (a Pending payment hasn't been confirmed
    /// to have actually happened yet, so it doesn't touch the invoice's balance).</summary>
    public void ApplyPayment(decimal amount)
    {
        if (amount > OutstandingBalance)
        {
            throw new PaymentExceedsOutstandingBalanceException(OutstandingBalance);
        }

        AmountPaid += amount;
        Status = AmountPaid >= TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }

    /// <summary><paramref name="hasVerifiedPayment"/> is computed by the caller (a cross-entity
    /// check — Payments aren't an owned navigation collection on Invoice, matching how
    /// MatchBankStatementLineCommandHandler checks cross-entity state rather than the entity
    /// itself holding a back-reference collection).</summary>
    public void Void(string reason, bool hasVerifiedPayment)
    {
        if (Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid))
        {
            throw new InvoiceNotVoidableException();
        }

        if (hasVerifiedPayment)
        {
            throw new InvoiceHasVerifiedPaymentException();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvoiceVoidRequiresReasonException();
        }

        Status = InvoiceStatus.Void;
        VoidReason = reason;
    }

    private void EnsureIsDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvoiceNotDraftException();
        }
    }
}
