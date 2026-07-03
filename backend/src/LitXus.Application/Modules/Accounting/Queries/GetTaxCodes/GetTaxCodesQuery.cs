using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetTaxCodes;

public record GetTaxCodesQuery : IRequest<IReadOnlyList<TaxCodeDto>>;
