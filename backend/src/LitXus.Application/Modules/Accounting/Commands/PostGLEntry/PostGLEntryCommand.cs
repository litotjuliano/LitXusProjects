using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.PostGLEntry;

public record PostGLEntryCommand(Guid GLEntryId) : IRequest<GLEntryDto>;
