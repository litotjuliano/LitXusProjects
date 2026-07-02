namespace LitXus.Application.Common.Interfaces;

public record LicenseKeyClaims(
    string ProductCode,
    string IssuedToCompany,
    IReadOnlyList<string> EnabledModules,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);

/// <summary>
/// Verifies a signed license key token (RS256 JWT) against the deployment's embedded public key —
/// see docs/16_Feature_Flags.md §16.4 and backend/tools/LitXus.LicenseGenerator.
/// </summary>
public interface ILicenseKeyVerifier
{
    /// <summary>Throws LicenseKeyInvalidException (bad signature / malformed / expired) on failure.</summary>
    LicenseKeyClaims Verify(string licenseKey);
}
