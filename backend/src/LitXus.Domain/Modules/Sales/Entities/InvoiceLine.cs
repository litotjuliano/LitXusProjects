using LitXus.Domain.Common;
using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Domain.Modules.Sales.Entities;

/// <summary>
/// No ProductId column yet — the schema's ProductId FK -> Products.Id can't exist until Phase 3
/// creates the Products table; every Phase 2 line is free-text (Description/Quantity/UnitPrice),
/// matching the schema's own "nullable if Inventory module not licensed" note taken to its Phase 2
/// conclusion (Inventory isn't licensed yet at all). UnitOfMeasure is a Phase 2-only free-text
/// convenience (e.g. "pcs", "kg", "box") — the schema only defines a real UnitOfMeasure on Products
/// (§2.4), so once Phase 3 adds a real Product catalog this becomes Product-driven instead of
/// freely typed per line.
/// </summary>
public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string? UnitOfMeasure { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public Guid? TaxCodeId { get; private set; }
    public TaxCode? TaxCode { get; private set; }

    private InvoiceLine() { }

    public static InvoiceLine Create(string description, decimal quantity, string? unitOfMeasure, decimal unitPrice, TaxCode? taxCode)
    {
        return new InvoiceLine
        {
            Description = description,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice,
            TaxCodeId = taxCode?.Id,
            TaxCode = taxCode,
        };
    }

    /// <summary>Computed, not persisted — Invoice aggregates this into its own SSTAmount column.</summary>
    public decimal ComputeTaxAmount() => TaxCode?.Calculate(LineTotal).TaxAmount ?? 0m;

    internal void AttachTo(Guid invoiceId) => InvoiceId = invoiceId;
}
