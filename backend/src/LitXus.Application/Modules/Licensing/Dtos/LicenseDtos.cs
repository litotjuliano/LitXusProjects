using LitXus.Domain.Modules.Shared.Entities;

namespace LitXus.Application.Modules.Licensing.Dtos;

public record LicenseDto(
    Guid Id,
    string ProductCode,
    string IssuedToCompany,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    string LicenseKey,
    IReadOnlyList<string> EnabledModules);

public static class LicenseMappingExtensions
{
    public static LicenseDto ToDto(this License license) => new(
        license.Id,
        license.ProductCode,
        license.IssuedToCompany,
        license.IssuedAtUtc,
        license.ExpiresAtUtc,
        license.LicenseKey,
        license.GetEnabledModuleList());
}
