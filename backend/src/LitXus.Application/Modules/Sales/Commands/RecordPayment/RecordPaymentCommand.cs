using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.RecordPayment;

public record RecordPaymentCommand(
    Guid InvoiceId,
    DateOnly PaymentDate,
    decimal Amount,
    string Method,
    string? ReferenceNumber,
    Guid? BankAccountId) : IRequest<PaymentDto>;
