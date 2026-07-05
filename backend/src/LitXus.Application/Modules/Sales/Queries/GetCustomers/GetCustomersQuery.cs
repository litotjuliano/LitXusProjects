using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetCustomers;

public record GetCustomersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CustomerDto>>;
