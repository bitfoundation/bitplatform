using System;
using System.Security.Cryptography.X509Certificates;

namespace Bit.Core.Contracts
{
    public interface IAppCertificatesProvider : IDisposable
    {
        X509Certificate2 GetSingleSignOnCertificate();
    }
}