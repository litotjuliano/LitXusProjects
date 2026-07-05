using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Events;

/// <summary>Raised by Invoice.Issue() — see docs/01_Architecture.md §1.4. Accounting-side
/// handlers subscribe to this without Sales ever referencing Accounting types.</summary>
public record InvoiceIssuedEvent(Guid InvoiceId) : IDomainEvent;
