using LitXus.Application.Modules.Company.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Company.Queries.GetSignatories;

public record GetSignatoriesQuery : IRequest<IReadOnlyList<CompanySignatoryDto>>;
