using LitXus.Application.Common.Events;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.EventHandlers;

/// <summary>Dr Cash/Bank, Cr Accounts Receivable — the mirror of PostInvoiceToGLHandler's
/// Dr Accounts Receivable posting. See docs/01_Architecture.md §1.4.</summary>
public class PostPaymentToGLHandler(
    IAppDbContext db,
    IFeatureFlagService featureFlagService,
    INumberSequenceGenerator numberSequenceGenerator,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<DomainEventNotification<PaymentVerifiedEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentVerifiedEvent> notification, CancellationToken cancellationToken)
    {
        if (!featureFlagService.IsEnabled(Module.Accounting))
        {
            return;
        }

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == notification.DomainEvent.PaymentId, cancellationToken);
        if (payment is null)
        {
            return;
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == payment.InvoiceId, cancellationToken);

        var settings = await db.SalesSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsConfigured)
        {
            throw new SalesSettingsNotConfiguredException();
        }

        Account cashAccount;
        if (payment.BankAccountId.HasValue)
        {
            var bankAccount = await db.BankAccounts.FirstAsync(b => b.Id == payment.BankAccountId.Value, cancellationToken);
            cashAccount = await db.Accounts.FirstAsync(a => a.Id == bankAccount.AccountId, cancellationToken);
        }
        else
        {
            cashAccount = await db.Accounts.FirstAsync(a => a.Id == settings.DefaultCashAccountId, cancellationToken);
        }

        var receivableAccount = await db.Accounts.FirstAsync(a => a.Id == settings.DefaultReceivableAccountId, cancellationToken);

        var description = $"Payment received - invoice {invoice?.InvoiceNumber}";
        var lines = new List<GLEntryLine>
        {
            GLEntryLine.Create(cashAccount, payment.Amount, 0m, description),
            GLEntryLine.Create(receivableAccount, 0m, payment.Amount, description),
        };

        var glEntry = GLEntry.CreateDraft(payment.PaymentDate, description, lines, GLEntrySource.SalesAutoPost, payment.Id);
        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
        glEntry.Post(entryNumber, currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        db.GLEntries.Add(glEntry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
