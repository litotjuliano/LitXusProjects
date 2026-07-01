using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;

public record CreateGLEntryLineInput(Guid AccountId, decimal DebitAmount, decimal CreditAmount, string? LineDescription);

public record CreateGLEntryCommand(
    DateOnly EntryDate,
    string Description,
    IReadOnlyList<CreateGLEntryLineInput> Lines) : IRequest<GLEntryDto>;
