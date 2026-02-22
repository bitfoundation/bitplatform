using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// For more information, checkout AppCertificate.md file in the root directory of the server project.
/// </summary>
public static class AppCertificateService
{
    private static X509Certificate2? appCert;

    /// <summary>
    /// This would return AppCertificate containing private key and public key.
    /// </summary>
    public static X509Certificate2 GetAppCertificate()
    {
        if (appCert is not null)
            return appCert;

        var certPath = Path.Combine(AppContext.BaseDirectory, "AppCertificate.Cert.pem");
        var keyPath = Path.Combine(AppContext.BaseDirectory, "AppCertificate.Private.pem");

        appCert = X509Certificate2.CreateFromPemFile(certPath, keyPath); // This would work even in restricted shared hosting environments where you don't have access to certificate store.
        // You could also use pfx file with password or using vaults such as Azure Key Vault etc.

        if (AppEnvironment.IsDevelopment() is false && appCert.Thumbprint is "189C12DB3EEF0A151E3F596DCD807CD2ECA0A26C")
            throw new InvalidOperationException("You are using the default self-signed certificate in non-development environment. Please use a secure certificate in production.");

        return appCert;
    }

    /// <summary>
    /// This would return the private key of app certificate to issue JWT tokens.
    /// </summary>
    public static RsaSecurityKey GetPrivateSecurityKey()
    {
        return new RsaSecurityKey(GetAppCertificate().GetRSAPrivateKey());
    }

    /// <summary>
    /// This would return the public key of app certificate to validate JWT tokens.
    /// </summary>
    public static RsaSecurityKey GetPublicSecurityKey()
    {
        return new RsaSecurityKey(GetAppCertificate().GetRSAPublicKey());
    }
}
