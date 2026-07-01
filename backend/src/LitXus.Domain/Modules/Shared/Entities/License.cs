using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Shared.Entities;

/// <summary>
/// One row per deployed instance. EnabledModules drives IFeatureFlagService — see docs/16_Feature_Flags.md.
/// </summary>
public class License : BaseEntity
{
    public string ProductCode { get; private set; } = string.Empty;
    public string EnabledModules { get; private set; } = string.Empty;
    public string IssuedToCompany { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string LicenseKey { get; private set; } = string.Empty;

    private License() { }

    public static License Create(string productCode, string enabledModules, string issuedToCompany, DateTime issuedAtUtc, DateTime expiresAtUtc, string licenseKey)
    {
        return new License
        {
            ProductCode = productCode,
            EnabledModules = enabledModules,
            IssuedToCompany = issuedToCompany,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            LicenseKey = licenseKey,
        };
    }

    public IReadOnlyList<string> GetEnabledModuleList() =>
        EnabledModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool IsExpired(DateTime utcNow) => utcNow > ExpiresAtUtc;
}
