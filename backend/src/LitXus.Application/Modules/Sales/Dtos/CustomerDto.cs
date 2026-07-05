using LitXus.Domain.Modules.Sales.Entities;

namespace LitXus.Application.Modules.Sales.Dtos;

public record CustomerDto(
    Guid Id,
    string Code,
    string CompanyName,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    decimal CreditLimit,
    int PaymentTermsDays,
    bool IsActive);

public static class CustomerMappingExtensions
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id, customer.Code, customer.CompanyName, customer.ContactPerson, customer.Email,
        customer.Phone, customer.Address, customer.CreditLimit, customer.PaymentTermsDays, customer.IsActive);
}
