using LitXus.Application.Modules.Licensing.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Licensing.Commands.ApplyLicenseKey;

public record ApplyLicenseKeyCommand(string LicenseKey) : IRequest<LicenseDto>;
