using System.IO;
using System.Security.Cryptography.X509Certificates;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Owin.Implementations
{
    public class DefaultCertificateProvider : ICertificateProvider
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual IPathProvider PathProvider { get; set; }

        private X509Certificate2 _certificate;

        public virtual X509Certificate2 GetSingleSignOnCertificate()
        {
            if (_certificate == null)
            {
                AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

                string password = activeAppEnvironment
                    .GetConfig<string>("IdentityCertificatePassword");

                _certificate = new X509Certificate2(File.ReadAllBytes(PathProvider.MapPath(activeAppEnvironment.GetConfig("IdentityServerCertificatePath", "IdentityServerCertificate.pfx"))),
                    password);
            }

            return _certificate;
        }

        public virtual void Dispose()
        {
            _certificate?.Dispose();
        }
    }
}