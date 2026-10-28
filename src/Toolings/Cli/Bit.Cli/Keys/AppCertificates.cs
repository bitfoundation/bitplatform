using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Keys;

public sealed record AppCertificatePair(string Certificate, string PrivateKey)
{
    public string Pem => Certificate + PrivateKey;
}

public sealed record AppCertificateInfo(string Name, string Subject, string Thumbprint, DateTimeOffset NotAfter, bool Active, string? Problem);

public static partial class AppCertificates
{
    public const string ActiveName = "AppCertificate";

    public const string DevelopmentSubject = "CN=AppCertificate, OU=Development";

    public const string ProductionSubject = "CN=AppCertificate, OU=Production";

    public static AppCertificatePair Create(string subject, TimeSpan validity)
    {
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.Add(validity));

        return new(certificate.ExportCertificatePem().ReplaceLineEndings("\n") + "\n", rsa.ExportPkcs8PrivateKeyPem().ReplaceLineEndings("\n") + "\n");
    }

    public static AppCertificatePair CreateDevelopment() => Create(DevelopmentSubject, TimeSpan.FromDays(5 * 365));

    public static AppCertificatePair CreateProduction() => Create(ProductionSubject, TimeSpan.FromDays(365));

    public static void Write(string directory, string name, AppCertificatePair pair)
    {
        KeyFolder.WriteSecret(Path.Combine(directory, $"{name}.key"), pair.PrivateKey);
        AtomicFile.WriteAllText(Path.Combine(directory, $"{name}.crt"), pair.Certificate);
    }

    public static AppCertificatePair? Read(string directory, string name)
    {
        var certificate = Path.Combine(directory, $"{name}.crt");
        var key = Path.Combine(directory, $"{name}.key");

        return File.Exists(certificate) && File.Exists(key) ? new(File.ReadAllText(certificate), File.ReadAllText(key)) : null;
    }

    public static AppCertificatePair? Parse(string pem)
    {
        var certificate = CertificateRegex().Match(pem);
        var key = PrivateKeyRegex().Match(pem);

        return certificate.Success && key.Success ? new(certificate.Value.ReplaceLineEndings("\n") + "\n", key.Value.ReplaceLineEndings("\n") + "\n") : null;
    }

    public static string Subject(AppCertificatePair pair)
    {
        using var certificate = X509Certificate2.CreateFromPem(pair.Certificate);
        return certificate.Subject;
    }

    public static string Retire(string directory, DateTimeOffset now)
    {
        var name = $"{ActiveName}.{now:yyyyMMdd}";

        for (var suffix = 2; File.Exists(Path.Combine(directory, $"{name}.crt")) || File.Exists(Path.Combine(directory, $"{name}.key")); suffix++)
        {
            name = $"{ActiveName}.{now:yyyyMMdd}-{suffix}";
        }

        File.Move(Path.Combine(directory, $"{ActiveName}.key"), Path.Combine(directory, $"{name}.key"));
        File.Move(Path.Combine(directory, $"{ActiveName}.crt"), Path.Combine(directory, $"{name}.crt"));
        return name;
    }

    public static IReadOnlyList<AppCertificateInfo> List(string directory)
    {
        var names = Directory.EnumerateFiles(directory, $"{ActiveName}*.crt")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(n => n == ActiveName || n.StartsWith($"{ActiveName}.", StringComparison.Ordinal))
            .OrderBy(n => n == ActiveName ? 0 : 1)
            .ThenBy(n => n, StringComparer.Ordinal);

        var infos = new List<AppCertificateInfo>();

        foreach (var name in names)
        {
            var crt = Path.Combine(directory, $"{name}.crt");
            var key = Path.Combine(directory, $"{name}.key");

            try
            {
                using var certificate = X509Certificate2.CreateFromPem(File.ReadAllText(crt));
                string? problem = File.Exists(key) is false ? $"{name}.key is missing" : null;

                if (problem is null)
                {
                    try
                    {
                        using var withKey = X509Certificate2.CreateFromPemFile(crt, key);
                    }
                    catch (CryptographicException)
                    {
                        problem = $"{name}.key isn't the private key of {name}.crt";
                    }
                }

                infos.Add(new(name, certificate.Subject, certificate.Thumbprint, new DateTimeOffset(certificate.NotAfter), name == ActiveName, problem));
            }
            catch (CryptographicException)
            {
                infos.Add(new(name, "", "", default, name == ActiveName, $"{name}.crt isn't a PEM certificate"));
            }
        }

        return infos;
    }

    public static byte[] ExportPfx(AppCertificatePair pair, string password)
    {
        using var certificate = X509Certificate2.CreateFromPem(pair.Certificate, pair.PrivateKey);
        return certificate.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, password);
    }

    [GeneratedRegex(@"-----BEGIN CERTIFICATE-----[\s\S]+?-----END CERTIFICATE-----")]
    private static partial Regex CertificateRegex();

    [GeneratedRegex(@"-----BEGIN (RSA )?PRIVATE KEY-----[\s\S]+?-----END (RSA )?PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();
}
