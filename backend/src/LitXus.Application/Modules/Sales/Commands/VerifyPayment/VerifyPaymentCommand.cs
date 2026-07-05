using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.VerifyPayment;

public record VerifyPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
