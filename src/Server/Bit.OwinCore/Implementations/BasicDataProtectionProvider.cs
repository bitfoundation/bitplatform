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

        private AppEnvironment _activeAppEnvironment;

        private IAppEnvironmentProvider _appEnvironmentProvider;

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(AppEnvironmentProvider));

                _appEnvironmentProvider = value;

                _activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();
            }
            get
            {
                return _appEnvironmentProvider;
            }
        }

        public virtual string[] Purposes { get; set; } = new string[] { };

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
                    writer.Write(_activeAppEnvironment.AppInfo.Name);
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

            return new BasicDataProtectionProvider
            {
                AppEnvironmentProvider = AppEnvironmentProvider,
                Purposes = purposes
            };
        }
    }
}
