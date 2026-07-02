using LitXus.Application.Modules.Company.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Company.Commands.AddSignatory;

public record AddSignatoryCommand(
    string Name,
    string IcNumber,
    string Position,
    string Email,
    string? Phone) : IRequest<CompanySignatoryDto>;
