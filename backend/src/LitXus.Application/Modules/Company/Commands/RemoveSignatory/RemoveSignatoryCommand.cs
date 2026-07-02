using MediatR;

namespace LitXus.Application.Modules.Company.Commands.RemoveSignatory;

public record RemoveSignatoryCommand(Guid SignatoryId) : IRequest;
