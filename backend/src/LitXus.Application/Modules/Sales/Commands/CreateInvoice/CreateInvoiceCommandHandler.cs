using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResultDto>
{
    public async Task<CreateInvoiceResultDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        if (!customer.IsActive)
        {
            throw new CustomerInactiveException(customer.Code);
        }

        var taxCodeIds = request.Lines.Where(l => l.TaxCodeId.HasValue).Select(l => l.TaxCodeId!.Value).Distinct().ToList();
        var taxCodes = await db.TaxCodes
            .Where(t => taxCodeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var lines = request.Lines.Select(l =>
        {
            TaxCode? taxCode = null;
            if (l.TaxCodeId.HasValue && !taxCodes.TryGetValue(l.TaxCodeId.Value, out taxCode))
            {
                throw new NotFoundException(nameof(TaxCode), l.TaxCodeId.Value);
            }

            return InvoiceLine.Create(l.Description, l.Quantity, l.UnitOfMeasure, l.UnitPrice, taxCode);
        }).ToList();

        var invoice = Invoice.CreateDraft(request.CustomerId, request.InvoiceDate, request.DueDate, request.Notes, lines);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        var warning = await BuildCreditLimitWarningAsync(customer, invoice, cancellationToken);
        var dto = invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
        return new CreateInvoiceResultDto(dto, warning);
    }

    /// <summary>Soft check only — never blocks creation. CreditLimit &lt;= 0 means no limit is
    /// configured for this customer, matching the field's default/unset value.</summary>
    private async Task<string?> BuildCreditLimitWarningAsync(Customer customer, Invoice invoice, CancellationToken cancellationToken)
    {
        if (customer.CreditLimit <= 0)
        {
            return null;
        }

        var existingOutstanding = await db.Invoices
            .Where(i => i.CustomerId == customer.Id && i.Id != invoice.Id
                && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid))
            .SumAsync(i => i.TotalAmount - i.AmountPaid, cancellationToken);

        var projectedOutstanding = existingOutstanding + invoice.TotalAmount;
        if (projectedOutstanding <= customer.CreditLimit)
        {
            return null;
        }

        return $"{customer.Code} would have RM {projectedOutstanding:N2} outstanding once this invoice is issued, exceeding their credit limit of RM {customer.CreditLimit:N2}.";
    }
}
