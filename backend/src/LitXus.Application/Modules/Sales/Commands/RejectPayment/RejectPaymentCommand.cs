using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.RejectPayment;

public record RejectPaymentCommand(Guid PaymentId, string Reason) : IRequest<PaymentDto>;
