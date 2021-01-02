using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bit.Owin.Implementations
{
    public class DefaultAppCertificatesProvider : IAppCertificatesProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;
        public virtual IPathProvider PathProvider { get; set; } = default!;

        private X509Certificate2? _certificate;
        private RSA? _rsa;

        public virtual X509Certificate2 GetSingleSignOnServerCertificate()
        {
            if (_certificate == null)
            {
                string password = AppEnvironment
                    .GetConfig<string>(AppEnvironment.KeyValues.IdentityCertificatePassword) ?? throw new InvalidOperationException($"{nameof(AppEnvironment.KeyValues.IdentityCertificatePassword)} is null.");

                byte[] pfxRaw = File.ReadAllBytes(PathProvider.MapPath(AppEnvironment.GetConfig(AppEnvironment.KeyValues.IdentityServerCertificatePath, AppEnvironment.KeyValues.IdentityServerCertificatePathDefaultValue)!));

                try
                {
                    _certificate = new X509Certificate2(pfxRaw, password);
                }
                catch
                {
                    _certificate = new X509Certificate2(pfxRaw, password, X509KeyStorageFlags.UserKeySet);
                }

                using X509Chain chain = new X509Chain();
                bool isValid = chain.Build(_certificate);
                if (!isValid && chain.ChainStatus.Any(s => s.Status == X509ChainStatusFlags.NotTimeValid))
                    throw new InvalidOperationException($"A required certificate is not within its validity period when verifying against the current system clock or the timestamp in the signed file.");
            }

            return _certificate;
        }

        public virtual RSA? GetSingleSignOnClientRsaKey()
        {
            if (_rsa == null)
            {
                if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityClientPublicKey, out string publicKey))
                {
                    _rsa = RSA.Create();

                    _rsa.FromXmlString(publicKey);
                }
                else
                {
                    _rsa = GetSingleSignOnServerCertificate().GetRSAPublicKey();
                }
            }

            return _rsa;
        }

        public virtual void Dispose()
        {
            _certificate?.Dispose();
            _rsa?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}