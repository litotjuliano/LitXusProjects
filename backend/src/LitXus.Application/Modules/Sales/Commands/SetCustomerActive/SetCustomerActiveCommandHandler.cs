using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Sales.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Sales.Commands.SetCustomerActive;

public class SetCustomerActiveCommandHandler(IAppDbContext db) : IRequestHandler<SetCustomerActiveCommand>
{
    public async Task Handle(SetCustomerActiveCommand request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        customer.SetActive(request.IsActive);
        await db.SaveChangesAsync(cancellationToken);
    }
}
