using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Shared.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LitXus.Infrastructure.Services;

/// <summary>Verifies license keys signed offline by backend/tools/LitXus.LicenseGenerator — see docs/16_Feature_Flags.md §16.4.</summary>
public class LicenseKeyVerifier : ILicenseKeyVerifier
{
    private readonly RsaSecurityKey _signingKey;

    public LicenseKeyVerifier(IOptions<LicensingOptions> options)
    {
        // Parsed once, held for the app's lifetime (registered as a singleton) — NOT per-call with a
        // `using`-disposed RSA instance. Microsoft.IdentityModel.Tokens caches SignatureProviders per
        // security key by default (CryptoProviderFactory.CacheSignatureProviders); disposing the RSA
        // object between calls left the cache holding a reference to a disposed key, making every
        // *second* verification against the same key fail unpredictably.
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(options.Value.PublicKeyPem);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Licensing:PublicKeyPem is misconfigured: {ex.Message}", ex);
        }
        _signingKey = new RsaSecurityKey(rsa);
    }

    public LicenseKeyClaims Verify(string licenseKey)
    {
        JwtSecurityToken jwt;
        try
        {
            new JwtSecurityTokenHandler().ValidateToken(licenseKey, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "LitXus",
                ValidateAudience = true,
                ValidAudience = "LitXus",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromMinutes(5),
            }, out var validatedToken);
            jwt = (JwtSecurityToken)validatedToken;
        }
        catch (Exception ex)
        {
            var reason = ex is SecurityTokenExpiredException
                ? "this key has expired."
                : "the signature is invalid or the key is malformed.";
            throw new LicenseKeyInvalidException(reason);
        }

        var productCode = jwt.Claims.FirstOrDefault(c => c.Type == "productCode")?.Value
            ?? throw new LicenseKeyInvalidException("missing productCode claim.");
        var company = jwt.Claims.FirstOrDefault(c => c.Type == "company")?.Value
            ?? throw new LicenseKeyInvalidException("missing company claim.");
        var modules = jwt.Claims.Where(c => c.Type == "module").Select(c => c.Value).ToList();

        return new LicenseKeyClaims(productCode, company, modules, jwt.IssuedAt.ToUniversalTime(), jwt.ValidTo.ToUniversalTime());
    }
}
