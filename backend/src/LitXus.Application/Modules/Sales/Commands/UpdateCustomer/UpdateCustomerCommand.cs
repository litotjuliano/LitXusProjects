using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    string CompanyName,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    decimal CreditLimit,
    int PaymentTermsDays) : IRequest<CustomerDto>;
