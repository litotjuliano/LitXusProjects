using LitXus.Application.Modules.Company.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Company.Queries.GetCompanyProfile;

public record GetCompanyProfileQuery : IRequest<CompanyDto?>;
