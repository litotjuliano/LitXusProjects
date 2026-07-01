using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.VoidGLEntry;

public record VoidGLEntryCommand(Guid GLEntryId, string Reason) : IRequest<GLEntryDto>;
