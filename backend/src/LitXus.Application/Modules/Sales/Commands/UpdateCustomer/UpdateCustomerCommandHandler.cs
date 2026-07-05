using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Dtos;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler(IAppDbContext db) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        customer.Update(
            request.CompanyName, request.ContactPerson, request.Email,
            request.Phone, request.Address, request.CreditLimit, request.PaymentTermsDays);

        await db.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }
}
