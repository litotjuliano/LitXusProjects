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
        "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm9kdWN0Q29kZSI6IkFjY291bnRpbmdQcm8iLCJjb21wYW55IjoiTGl0WHVzIERlbW8gVHJhZGluZyBTZG4gQmhkIiwiaWF0IjoxNzgzMjU5Mjg5LCJtb2R1bGUiOlsiQWNjb3VudGluZyIsIlNhbGVzIl0sIm5iZiI6MTc4MzI1OTI4OSwiZXhwIjoxODE0NDg2NDAwLCJpc3MiOiJMaXRYdXMiLCJhdWQiOiJMaXRYdXMifQ.SlKXy1-FGK7VwXL3o5Kr0hzTIjem_eqwUkGVvSBMaAYZb21QlpE9l-XXtD1Gxf_Hr3jwNPFT368TpzKh6wD_CM3Nw3llCwePyvCAmLabPPonTAYaZ6lADuri7msaS9W9La2Qtg1e1-D8Atp3mgGe2MKiTgw45VUYzBNjon1ctLmHIjIIuKuVgPd7UVgq1UoOpFwGunw8OSAmnCtECdRfU_O7H6ChWyfjPAfg887XEb8btvFp5h3yaKq7OmtOp2ZFm7fSZVjdIXluwBG3Tj2kh2vQg1bzDZ1GsDsz80cH5MgdvUzFTZLXyIkTFpfgRrpjwWUKs29yZgVMF1w5trAjBg";

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
