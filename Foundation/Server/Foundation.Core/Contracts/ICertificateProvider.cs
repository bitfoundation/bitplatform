using System;
using System.Security.Cryptography.X509Certificates;

namespace Foundation.Core.Contracts
{
    public interface ICertificateProvider : IDisposable
    {
        X509Certificate2 GetSingleSignOnCertificate();
    }
}