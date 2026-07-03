using LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;
using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.UpdateGLEntry;

public record UpdateGLEntryCommand(
    Guid Id,
    DateOnly EntryDate,
    string Description,
    IReadOnlyList<CreateGLEntryLineInput> Lines) : IRequest<GLEntryDto>;
