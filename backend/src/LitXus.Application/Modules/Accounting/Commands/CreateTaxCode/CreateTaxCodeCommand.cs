using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.CreateTaxCode;

public record CreateTaxCodeCommand(string Code, string Name, decimal Rate, string Type) : IRequest<TaxCodeDto>;
