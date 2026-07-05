using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetPayments;

public record GetPaymentsQuery(string? Status = null) : IRequest<IReadOnlyList<PaymentDto>>;
