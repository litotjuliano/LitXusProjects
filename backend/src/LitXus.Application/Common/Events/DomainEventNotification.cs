using LitXus.Domain.Common;
using MediatR;

namespace LitXus.Application.Common.Events;

/// <summary>
/// Wraps a framework-free IDomainEvent (Domain layer) as a MediatR INotification, so Domain
/// never needs to reference MediatR — see docs/01_Architecture.md §1.4. Published by
/// DomainEventDispatchInterceptor (Infrastructure) via reflection, one per queued domain event.
/// </summary>
public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
