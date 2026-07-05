using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Events;

/// <summary>Raised by CreditNote.Create() — see docs/01_Architecture.md §1.4.</summary>
public record CreditNoteAppliedEvent(Guid CreditNoteId) : IDomainEvent;
