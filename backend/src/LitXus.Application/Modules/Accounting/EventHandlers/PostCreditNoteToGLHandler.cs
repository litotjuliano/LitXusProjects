using LitXus.Application.Common.Events;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.EventHandlers;

/// <summary>Dr Sales Revenue, Cr Accounts Receivable — the reverse of PostInvoiceToGLHandler's
/// revenue posting. CreditNote has no per-line tax breakdown (unlike Invoice/InvoiceLine), so the
/// full Amount is reversed against Revenue only; SST Payable is not adjusted. See
/// docs/01_Architecture.md §1.4 and docs/phase-2-sales/Business_Rules.md.</summary>
public class PostCreditNoteToGLHandler(
    IAppDbContext db,
    IFeatureFlagService featureFlagService,
    INumberSequenceGenerator numberSequenceGenerator,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<DomainEventNotification<CreditNoteAppliedEvent>>
{
    public async Task Handle(DomainEventNotification<CreditNoteAppliedEvent> notification, CancellationToken cancellationToken)
    {
        if (!featureFlagService.IsEnabled(Module.Accounting))
        {
            return;
        }

        var creditNote = await db.CreditNotes.FirstOrDefaultAsync(c => c.Id == notification.DomainEvent.CreditNoteId, cancellationToken);
        if (creditNote is null)
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

        var description = $"Credit note {creditNote.CreditNoteNumber}";
        var lines = new List<GLEntryLine>
        {
            GLEntryLine.Create(revenueAccount, creditNote.Amount, 0m, description),
            GLEntryLine.Create(receivableAccount, 0m, creditNote.Amount, description),
        };

        var glEntry = GLEntry.CreateDraft(DateOnly.FromDateTime(dateTimeProvider.UtcNow), description, lines, GLEntrySource.SalesAutoPost, creditNote.Id);
        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
        glEntry.Post(entryNumber, currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        db.GLEntries.Add(glEntry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
