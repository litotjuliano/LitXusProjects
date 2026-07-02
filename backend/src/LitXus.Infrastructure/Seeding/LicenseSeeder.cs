using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Seeds a local/demo Accounting Pro license — a real RS256-signed token generated via
/// backend/tools/LitXus.LicenseGenerator against the dev keypair (public half lives in
/// appsettings.Development.json's Licensing:PublicKeyPem), verified here exactly the same way a
/// Super-Admin-pasted key would be — so ProductCode/IssuedToCompany/EnabledModules/expiry all come
/// from the token itself, not duplicated as separate seed values. See docs/16_Feature_Flags.md §16.4.
/// </summary>
public class LicenseSeeder(IAppDbContext db, ILicenseKeyVerifier licenseKeyVerifier) : ISeeder
{
    public int Order => 3;

    private const string DevLicenseKey =
        "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm9kdWN0Q29kZSI6IkFjY291bnRpbmdQcm8iLCJjb21wYW55IjoiTGl0WHVzIERlbW8gVHJhZGluZyBTZG4gQmhkIiwiaWF0IjoxNzgzMDA5MDI4LCJtb2R1bGUiOiJBY2NvdW50aW5nIiwibmJmIjoxNzgzMDA5MDI4LCJleHAiOjE4MTQ0ODY0MDAsImlzcyI6IkxpdFh1cyIsImF1ZCI6IkxpdFh1cyJ9.CYlAdMVnWPtgELd9LS0tSHV7sYVriialBuzwOk8rzvo8fBju1_jo93SmOTM2-h8Xp4o69RVII2625hAonOB04-u0hPXWmevcMuIYXmOit8dQ8hrfR1-FVFJn0kTuoui6fxHl40ksYTFsKWbl1pf-7s2GQ4XgYq_n0j2sFy4N3PFixPsyXkzRm8oZvAQ-EDNuRa8Fmg356YnQIzs-APaXhpnr1k_mCORGhUyAd-C812bZBCH66aiaZ5SKyR0Hqo0fBGMBQZ6B8HLqcfncgUyC-EmQ7JS_SfHW7xGjjykTfCnPUccaeqkNLONt4WCGUE1CcVRgwLmoWoyxKyDB9pRCCQ";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Licenses.AnyAsync(cancellationToken))
        {
            return;
        }

        var claims = licenseKeyVerifier.Verify(DevLicenseKey);

        var license = License.Create(
            productCode: claims.ProductCode,
            enabledModules: string.Join(",", claims.EnabledModules),
            issuedToCompany: claims.IssuedToCompany,
            issuedAtUtc: claims.IssuedAtUtc,
            expiresAtUtc: claims.ExpiresAtUtc,
            licenseKey: DevLicenseKey);

        db.Licenses.Add(license);
        await db.SaveChangesAsync(cancellationToken);
    }
}
