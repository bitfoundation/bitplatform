using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// For more information, checkout AppCertificate.md file in the root directory of the server project.
/// </summary>
public static class AppCertificateService
{
    private const string ActiveCertificateName = "AppCertificate";

    /// <summary>
    /// The self-signed certificate this template ships with. Its private key is public, so anyone could mint a valid
    /// token or decrypt the Data Protection key ring - it is a local development convenience and nothing else.
    /// </summary>
    private const string TestCertificateThumbprint = "1D549B7F8B0D52A54DE1C36948055B17C90063A2";

    private static X509Certificate2[]? allAppCerts;
    private static RsaSecurityKey? privateSecurityKey;
    private static RsaSecurityKey[]? publicSecurityKeys;

    /// <summary>
    /// The active certificate first, followed by any retired ones.
    /// A retired certificate is any <c>AppCertificate.{anything}.crt</c> + <c>.key</c> pair sitting next to the active
    /// one; dropping a pair there is the whole rotation procedure (See AppCertificate.md).
    /// </summary>
    public static X509Certificate2[] GetAllAppCertificates(IConfiguration configuration)
    {
        if (allAppCerts is not null)
            return allAppCerts;

        var certs = new List<X509Certificate2> { LoadCertificate(ActiveCertificateName) };

        // Ordered so that a rotation looks the same on every instance; the active one is prepended above either way.
        foreach (var retiredCertPath in Directory.GetFiles(AppContext.BaseDirectory, $"{ActiveCertificateName}.*.crt").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(retiredCertPath);

            if (name is ActiveCertificateName)
                continue; // Already loaded above.

            certs.Add(LoadCertificate(name));
        }

        // Retired certificates are checked too: a retired default would still be trusted for validation, so its
        // public private key could still be used to mint tokens this server accepts.
        if (AppEnvironment.IsDevelopment() is false && certs.Any(cert => cert.Thumbprint is TestCertificateThumbprint))
            throw new InvalidOperationException("You are using the default self-signed certificate in non-development environment. Generate and use your own certificate using `openssl genrsa` and `openssl req` commands described in AppCertificate.md file.");

        return allAppCerts = [.. certs];
    }

    /// <summary>
    /// The one certificate that still does the writing: it signs new tokens and the Data Protection key ring is
    /// encrypted to it. Retired certificates only ever read - validating a token, decrypting an existing key ring
    /// entry - which is what lets them be dropped once nothing they wrote is alive any more.
    /// </summary>
    public static X509Certificate2 GetActiveAppCertificate(IConfiguration configuration)
    {
        return GetAllAppCertificates(configuration)[0];
    }

    private static X509Certificate2 LoadCertificate(string name)
    {
        var certPemFilePath = Path.Combine(AppContext.BaseDirectory, $"{name}.crt");
        var keyPemFilePath = Path.Combine(AppContext.BaseDirectory, $"{name}.key");

        return X509Certificate2.CreateFromPemFile(certPemFilePath, keyPemFilePath);

        // Load pfx file sample:
        // var pfxFilePath = Path.Combine(AppContext.BaseDirectory, $"{name}.pfx");
        // return X509CertificateLoader.LoadPkcs12FromFile(pfxFilePath, configuration["Identity:CertificatePassword"]);
    }

    /// <summary>
    /// This would return the private key of the active certificate to issue JWT tokens.
    /// </summary>
    public static RsaSecurityKey GetPrivateSecurityKey(IConfiguration configuration)
    {
        return privateSecurityKey ??= ToSecurityKey(GetActiveAppCertificate(configuration), publicOnly: false);
    }

    /// <summary>
    /// The public keys every token issued by this server may have been signed with - the active certificate's plus
    /// every retired one's. They are published as-is at <c>/.well-known/jwks</c> and handed to
    /// <c>TokenValidationParameters.IssuerSigningKeys</c>, so a token minted before a rotation keeps validating until
    /// its certificate is removed.
    /// </summary>
    public static RsaSecurityKey[] GetPublicSecurityKeys(IConfiguration configuration)
    {
        return publicSecurityKeys ??= [.. GetAllAppCertificates(configuration).Select(cert => ToSecurityKey(cert, publicOnly: true))];
    }

    /// <summary>
    /// The certificate's thumbprint becomes the key's <c>kid</c>, in the JWT header and in the published JWKS alike.
    /// That is what lets a validator pick the right key out of several, which is the whole basis of a rotation with
    /// no sign-out: a constant <c>kid</c> would make two certificates indistinguishable.
    /// </summary>
    private static RsaSecurityKey ToSecurityKey(X509Certificate2 cert, bool publicOnly)
    {
        var rsa = publicOnly
            ? cert.GetRSAPublicKey() ?? throw new InvalidOperationException("Public key not found in the certificate.")
            : cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("Private key not found in the certificate.");

        return new RsaSecurityKey(rsa) { KeyId = cert.Thumbprint };
    }
}
