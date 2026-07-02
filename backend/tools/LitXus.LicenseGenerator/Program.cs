// LitXus License Generator — run this OFFLINE, vendor-side only.
//
// The private key produced by `generate-keypair` must NEVER be committed to source control or
// shipped with a customer deployment. Only the public key half goes into a deployed app's config
// (appsettings.json -> Licensing:PublicKeyPem). This tool deliberately shares no code with
// LitXus.Application/Infrastructure — a real vendor-side tool shouldn't need to be rebuilt in
// lockstep with a customer's deployed app.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "generate-keypair" => GenerateKeyPair(args[1..]),
    "generate-license" => GenerateLicense(args[1..]),
    _ => Fail(),
};

static int Fail()
{
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Usage:
          generate-keypair --out-dir <dir>
          generate-license --private-key <path> --product <code> --company <name> --modules <csv> --expires <yyyy-MM-dd>

        Valid modules: Accounting, Sales, Inventory
        """);
}

static string? GetArg(string[] args, string name)
{
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static int GenerateKeyPair(string[] args)
{
    var outDir = GetArg(args, "--out-dir") ?? ".";
    Directory.CreateDirectory(outDir);

    using var rsa = RSA.Create(2048);
    var privatePem = rsa.ExportRSAPrivateKeyPem();
    var publicPem = rsa.ExportSubjectPublicKeyInfoPem();

    var privatePath = Path.Combine(outDir, "license-private.pem");
    var publicPath = Path.Combine(outDir, "license-public.pem");
    File.WriteAllText(privatePath, privatePem);
    File.WriteAllText(publicPath, publicPem);

    Console.WriteLine($"Private key: {privatePath} — keep this OFFLINE, never commit or deploy it.");
    Console.WriteLine($"Public key:  {publicPath} — this goes into a deployment's appsettings Licensing:PublicKeyPem.");
    return 0;
}

static int GenerateLicense(string[] args)
{
    var privateKeyPath = GetArg(args, "--private-key");
    var product = GetArg(args, "--product");
    var company = GetArg(args, "--company");
    var modulesCsv = GetArg(args, "--modules");
    var expiresStr = GetArg(args, "--expires");

    if (privateKeyPath is null || product is null || company is null || modulesCsv is null || expiresStr is null)
    {
        PrintUsage();
        return 1;
    }

    if (!DateTime.TryParse(expiresStr, out var expiresAtUtc))
    {
        Console.Error.WriteLine($"Could not parse --expires '{expiresStr}' as a date.");
        return 1;
    }

    var validModules = new[] { "Accounting", "Sales", "Inventory" };
    var modules = modulesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var invalidModules = modules.Where(m => !validModules.Contains(m)).ToList();
    if (invalidModules.Count > 0)
    {
        Console.Error.WriteLine($"Unknown module(s): {string.Join(", ", invalidModules)}. Valid: {string.Join(", ", validModules)}");
        return 1;
    }

    using var rsa = RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(privateKeyPath));

    var expiresUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
    // Allow generating an already-expired token (useful for testing expiry rejection) — issued/nbf
    // must never be after exp, so back-date them a day before an already-past expiry instead of
    // always using "now".
    var issuedAt = expiresUtc <= DateTime.UtcNow ? expiresUtc.AddDays(-1) : DateTime.UtcNow;
    var claims = new List<Claim>
    {
        new("productCode", product),
        new("company", company),
        new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64),
    };
    claims.AddRange(modules.Select(m => new Claim("module", m)));

    var token = new JwtSecurityToken(
        issuer: "LitXus",
        audience: "LitXus",
        claims: claims,
        notBefore: issuedAt,
        expires: expiresUtc,
        signingCredentials: new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256));

    Console.WriteLine(new JwtSecurityTokenHandler().WriteToken(token));
    return 0;
}
