using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class DefaultAppCertificatesProvider : IAppCertificatesProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IPathProvider PathProvider { get; set; }

        private X509Certificate2 _certificate;

        public virtual X509Certificate2 GetSingleSignOnCertificate()
        {
            if (_certificate == null)
            {
                string password = AppEnvironment
                    .GetConfig<string>("IdentityCertificatePassword");

                _certificate = new X509Certificate2(File.ReadAllBytes(PathProvider.MapPath(AppEnvironment.GetConfig("IdentityServerCertificatePath", "IdentityServerCertificate.pfx"))),
                    password);
            }

            return _certificate;
        }

        public virtual void Dispose()
        {
            _certificate?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}