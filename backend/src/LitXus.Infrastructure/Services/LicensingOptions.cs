namespace LitXus.Infrastructure.Services;

public class LicensingOptions
{
    public const string SectionName = "Licensing";

    /// <summary>RSA public key (PEM, SubjectPublicKeyInfo) used to verify license keys — the matching
    /// private key lives only in backend/tools/LitXus.LicenseGenerator, run offline.</summary>
    public string PublicKeyPem { get; set; } = string.Empty;
}
