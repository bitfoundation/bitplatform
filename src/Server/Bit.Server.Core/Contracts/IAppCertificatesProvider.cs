using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// Provides certificate which are required in common scenarios.
    /// </summary>
    public interface IAppCertificatesProvider : IDisposable
    {
        /// <summary>
        /// Provides certificate which signs JWT tokens.
        /// </summary>
        X509Certificate2 GetSingleSignOnServerCertificate();

        RSA GetSingleSignOnClientRsaKey();
    }
}