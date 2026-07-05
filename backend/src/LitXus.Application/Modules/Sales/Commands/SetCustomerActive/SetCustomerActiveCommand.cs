using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.SetCustomerActive;

public record SetCustomerActiveCommand(Guid Id, bool IsActive) : IRequest;
