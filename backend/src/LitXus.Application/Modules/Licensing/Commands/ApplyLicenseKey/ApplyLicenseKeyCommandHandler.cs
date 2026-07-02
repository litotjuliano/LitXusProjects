using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Licensing.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LicenseEntity = LitXus.Domain.Modules.Shared.Entities.License;

namespace LitXus.Application.Modules.Licensing.Commands.ApplyLicenseKey;

public class ApplyLicenseKeyCommandHandler(IAppDbContext db, ILicenseKeyVerifier licenseKeyVerifier, IFeatureFlagService featureFlagService)
    : IRequestHandler<ApplyLicenseKeyCommand, LicenseDto>
{
    public async Task<LicenseDto> Handle(ApplyLicenseKeyCommand request, CancellationToken cancellationToken)
    {
        var claims = licenseKeyVerifier.Verify(request.LicenseKey);

        var license = await db.Licenses.OrderByDescending(l => l.IssuedAtUtc).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(LicenseEntity), "current");

        license.ApplyVerifiedKey(
            claims.ProductCode,
            claims.IssuedToCompany,
            claims.EnabledModules,
            claims.IssuedAtUtc,
            claims.ExpiresAtUtc,
            request.LicenseKey);

        await db.SaveChangesAsync(cancellationToken);
        await featureFlagService.InvalidateAsync(cancellationToken);

        return license.ToDto();
    }
}
