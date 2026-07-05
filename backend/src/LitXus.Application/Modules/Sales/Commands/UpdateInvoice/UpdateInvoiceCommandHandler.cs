using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.UpdateInvoice;

public class UpdateInvoiceCommandHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines).ThenInclude(l => l.TaxCode)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);

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

        invoice.UpdateLines(request.InvoiceDate, request.DueDate, request.Notes, lines);

        // Same EF Core Added-vs-Modified fix as UpdateGLEntryCommandHandler — client-generated
        // GUID PKs on lines added purely via the parent's navigation collection get misidentified
        // as Modified instead of Added.
        db.InvoiceLines.AddRange(lines);

        await db.SaveChangesAsync(cancellationToken);

        return invoice.ToDto(customer, DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}
