using LitXus.Application.Common.Events;
using LitXus.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LitXus.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dispatches queued BaseEntity.DomainEvents via MediatR after a save actually commits — not
/// before, so a handler never reacts to a change that turned out to be rolled back. This is the
/// cross-module integration point docs/01_Architecture.md §1.4 describes (e.g. Sales.InvoiceIssuedEvent
/// -> Accounting's GL-posting handler) — Sales/Inventory never reference Accounting types directly,
/// they just publish an event that Accounting-side handlers (auto-discovered by MediatR) subscribe to.
/// </summary>
public class DomainEventDispatchInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await DispatchAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchAsync(Microsoft.EntityFrameworkCore.DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
        }
    }
}
