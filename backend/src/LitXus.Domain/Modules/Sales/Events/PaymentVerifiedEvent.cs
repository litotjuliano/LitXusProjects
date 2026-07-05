using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Events;

/// <summary>Raised by Payment.Verify() — see docs/01_Architecture.md §1.4.</summary>
public record PaymentVerifiedEvent(Guid PaymentId) : IDomainEvent;
