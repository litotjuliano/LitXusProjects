namespace LitXus.Domain.Common;

/// <summary>Marker for a plain, framework-free domain event — Domain must never reference MediatR
/// directly. DomainEventDispatchInterceptor (Infrastructure) wraps these in a generic MediatR
/// notification before publishing — see docs/01_Architecture.md §1.4.</summary>
public interface IDomainEvent
{
}
