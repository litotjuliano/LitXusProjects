using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.CreateCustomer;

public class CreateCustomerCommandHandler(IAppDbContext db) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await db.Customers.AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new CustomerCodeDuplicateException(request.Code);
        }

        var customer = Customer.Create(
            request.Code, request.CompanyName, request.ContactPerson, request.Email,
            request.Phone, request.Address, request.CreditLimit, request.PaymentTermsDays);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }
}
