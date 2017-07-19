using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin.Security.DataProtection;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Bit.OwinCore.Implementations
{
    public class BasicDataProtectionProvider : IDataProtector, IDataProtectionProvider
    {
        private const string PRIMARY_PURPOSE = "Microsoft.Owin.Security.IDataProtector";

        private readonly DataProtectionScope _dataProtectionScope = DataProtectionScope.CurrentUser;
        private readonly AppEnvironment _appEnvironment;
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        public virtual string[] Purposes { get; set; } = new string[] { };

#if DEBUG
        protected BasicDataProtectionProvider()
        {
        }
#endif

        public BasicDataProtectionProvider(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _appEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        public virtual byte[] Protect(byte[] userData)
        {
            return ProtectedData.Protect(userData, GetEntropy(), _dataProtectionScope);
        }

        public virtual byte[] Unprotect(byte[] protectedData)
        {
            return ProtectedData.Unprotect(protectedData, GetEntropy(), _dataProtectionScope);
        }

        private byte[] GetEntropy()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, sha256, CryptoStreamMode.Write))
                using (StreamWriter writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(_appEnvironment.AppInfo.Name);
                    writer.Write(PRIMARY_PURPOSE);

                    foreach (string purpose in Purposes)
                    {
                        writer.Write(purpose);
                    }
                }
                return sha256.Hash;
            }
        }

        public virtual IDataProtector Create(string[] purposes)
        {
            if (purposes == null)
                throw new ArgumentNullException(nameof(purposes));

            return new BasicDataProtectionProvider(_appEnvironmentProvider)
            {
                Purposes = purposes
            };
        }
    }
}
