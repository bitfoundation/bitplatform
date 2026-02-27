using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// For more information, checkout AppCertificate.md file in the root directory of the server project.
/// </summary>
public static class AppCertificateService
{
    private static X509Certificate2? appCert;
    private static RsaSecurityKey? privateSecurityKey;
    private static RsaSecurityKey? publicSecurityKey;

    /// <summary>
    /// This would return AppCertificate containing private key and public key.
    /// </summary>
    public static X509Certificate2 GetAppCertificate(IConfiguration configuration)
    {
        if (appCert is not null)
            return appCert;

        var keyPemFilePath = Path.Combine(AppContext.BaseDirectory, "AppCertificate.key");
        var certPemFilePath = Path.Combine(AppContext.BaseDirectory, "AppCertificate.crt");

        appCert = X509Certificate2.CreateFromPemFile(certPemFilePath, keyPemFilePath);

        // Load pfx file sample:
        // var pfxFilePath = Path.Combine(AppContext.BaseDirectory, "AppCertificate.pfx");
        // appCert = X509CertificateLoader.LoadPkcs12FromFile(pfxFilePath, configuration["Identity:CertificatePassword"]);

        if (AppEnvironment.IsDevelopment() is false && appCert.Thumbprint is "1D549B7F8B0D52A54DE1C36948055B17C90063A2")
            throw new InvalidOperationException("You are using the default self-signed certificate in non-development environment. Generate and use your own certificate using `openssl genrsa` and `openssl req` commands described in AppCertificate.md file.");

        return appCert;
    }

    /// <summary>
    /// This would return the private key of app certificate to issue JWT tokens.
    /// </summary>
    public static RsaSecurityKey GetPrivateSecurityKey(IConfiguration configuration)
    {
        return privateSecurityKey ??= new RsaSecurityKey(GetAppCertificate(configuration).GetRSAPrivateKey() ?? throw new InvalidOperationException("Private key not found in the certificate.")) { KeyId = "Boilerplate" };
    }

    /// <summary>
    /// This would return the public key of app certificate to validate JWT tokens.
    /// </summary>
    public static RsaSecurityKey GetPublicSecurityKey(IConfiguration configuration)
    {
        return publicSecurityKey ??= new RsaSecurityKey(GetAppCertificate(configuration).GetRSAPublicKey() ?? throw new InvalidOperationException("Public key not found in the certificate.")) { KeyId = "Boilerplate" };
    }
}
