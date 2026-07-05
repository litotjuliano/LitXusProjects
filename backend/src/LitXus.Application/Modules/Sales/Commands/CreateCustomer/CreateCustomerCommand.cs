using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Code,
    string CompanyName,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    decimal CreditLimit,
    int PaymentTermsDays) : IRequest<CustomerDto>;
