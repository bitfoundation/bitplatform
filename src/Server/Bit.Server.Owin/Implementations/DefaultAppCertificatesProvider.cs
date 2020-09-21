using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Bit.Owin.Implementations
{
    public class DefaultAppCertificatesProvider : IAppCertificatesProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;
        public virtual IPathProvider PathProvider { get; set; } = default!;

        private X509Certificate2? _certificate;

        public virtual X509Certificate2 GetSingleSignOnCertificate()
        {
            if (_certificate == null)
            {
                string password = AppEnvironment
                    .GetConfig<string>(AppEnvironment.KeyValues.IdentityCertificatePassword) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.IdentityCertificatePassword)} is null.");

                _certificate = new X509Certificate2(File.ReadAllBytes(PathProvider.MapPath(AppEnvironment.GetConfig(AppEnvironment.KeyValues.IdentityServerCertificatePath, AppEnvironment.KeyValues.IdentityServerCertificatePathDefaultValue)!)),
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