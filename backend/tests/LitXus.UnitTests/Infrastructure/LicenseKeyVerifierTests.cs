using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentAssertions;
using LitXus.Domain.Modules.Shared.Exceptions;
using LitXus.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LitXus.UnitTests.Infrastructure;

/// <summary>
/// Exercises the exact class of bug that broke license activation earlier this project (a mismatched
/// public/private keypair silently produced "the signature is invalid or the key is malformed" for
/// every key). Generates its own keypair and signs tokens the same way
/// backend/tools/LitXus.LicenseGenerator does, so it doesn't depend on any file on disk.
/// </summary>
public class LicenseKeyVerifierTests
{
    private static string SignToken(RSA signingKey, DateTime issuedAt, DateTime expiresAt, params string[] modules)
    {
        var claims = new List<Claim>
        {
            new("productCode", "AccountingPro"),
            new("company", "Acme Sdn Bhd"),
        };
        claims.AddRange(modules.Select(m => new Claim("module", m)));

        var token = new JwtSecurityToken(
            issuer: "LitXus",
            audience: "LitXus",
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static LicenseKeyVerifier BuildVerifier(RSA publicKeySource)
    {
        var publicKeyPem = publicKeySource.ExportSubjectPublicKeyInfoPem();
        var options = Options.Create(new LicensingOptions { PublicKeyPem = publicKeyPem });
        return new LicenseKeyVerifier(options);
    }

    [Fact]
    public void Verify_WithTokenSignedByMatchingPrivateKey_ReturnsClaims()
    {
        using var rsa = RSA.Create(2048);
        var verifier = BuildVerifier(rsa);
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddYears(1);
        var token = SignToken(rsa, issuedAt, expiresAt, "Accounting", "Sales");

        var claims = verifier.Verify(token);

        claims.ProductCode.Should().Be("AccountingPro");
        claims.IssuedToCompany.Should().Be("Acme Sdn Bhd");
        claims.EnabledModules.Should().BeEquivalentTo(["Accounting", "Sales"]);
    }

    [Fact]
    public void Verify_WithTokenSignedByDifferentKeypair_ThrowsLicenseKeyInvalidException()
    {
        using var verifierKey = RSA.Create(2048);
        using var wrongSigningKey = RSA.Create(2048);
        var verifier = BuildVerifier(verifierKey);
        var token = SignToken(wrongSigningKey, DateTime.UtcNow, DateTime.UtcNow.AddYears(1), "Accounting");

        var act = () => verifier.Verify(token);

        act.Should().Throw<LicenseKeyInvalidException>()
            .WithMessage("*signature is invalid*");
    }

    [Fact]
    public void Verify_WithExpiredToken_ThrowsLicenseKeyInvalidException()
    {
        using var rsa = RSA.Create(2048);
        var verifier = BuildVerifier(rsa);
        var token = SignToken(rsa, DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddDays(-1), "Accounting");

        var act = () => verifier.Verify(token);

        act.Should().Throw<LicenseKeyInvalidException>()
            .WithMessage("*expired*");
    }
}
