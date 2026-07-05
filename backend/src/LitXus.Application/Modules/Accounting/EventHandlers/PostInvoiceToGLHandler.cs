using LitXus.Application.Common.Events;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.EventHandlers;

/// <summary>
/// Accounting-side reaction to a Sales domain event — see docs/01_Architecture.md §1.4. Sales
/// never references this class or anything in it; it's auto-discovered by MediatR's assembly
/// scan and simply no-ops if Accounting isn't licensed, so Sales works standalone either way.
/// </summary>
public class PostInvoiceToGLHandler(
    IAppDbContext db,
    IFeatureFlagService featureFlagService,
    INumberSequenceGenerator numberSequenceGenerator,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<DomainEventNotification<InvoiceIssuedEvent>>
{
    public async Task Handle(DomainEventNotification<InvoiceIssuedEvent> notification, CancellationToken cancellationToken)
    {
        if (!featureFlagService.IsEnabled(Module.Accounting))
        {
            return;
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == notification.DomainEvent.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        var settings = await db.SalesSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsConfigured)
        {
            throw new SalesSettingsNotConfiguredException();
        }

        var receivableAccount = await db.Accounts.FirstAsync(a => a.Id == settings.DefaultReceivableAccountId, cancellationToken);
        var revenueAccount = await db.Accounts.FirstAsync(a => a.Id == settings.DefaultRevenueAccountId, cancellationToken);

        var description = $"Sales invoice {invoice.InvoiceNumber}";
        var lines = new List<GLEntryLine>
        {
            GLEntryLine.Create(receivableAccount, invoice.TotalAmount, 0m, description),
            GLEntryLine.Create(revenueAccount, 0m, invoice.SubTotal, description),
        };

        if (invoice.SSTAmount > 0)
        {
            var taxPayableAccount = await db.Accounts.FirstAsync(a => a.Id == settings.DefaultTaxPayableAccountId, cancellationToken);
            lines.Add(GLEntryLine.Create(taxPayableAccount, 0m, invoice.SSTAmount, description));
        }

        var glEntry = GLEntry.CreateDraft(invoice.InvoiceDate, description, lines, GLEntrySource.SalesAutoPost, invoice.Id);
        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
        glEntry.Post(entryNumber, currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        db.GLEntries.Add(glEntry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
