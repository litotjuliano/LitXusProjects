using LitXus.Application.Modules.Licensing.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Licensing.Queries.GetLicense;

public record GetLicenseQuery : IRequest<LicenseDto>;
