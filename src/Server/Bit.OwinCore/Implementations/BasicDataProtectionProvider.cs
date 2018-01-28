using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.Owin.Security.DataProtection;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;

namespace Bit.OwinCore.Implementations
{
    public class BasicDataProtectionProvider : IDataProtectionProvider
    {
        private IAppEnvironmentProvider _AppEnvironmentProvider;
        private AppEnvironment _App;

        public virtual IAppEnvironmentProvider AppEnvironmentProvider
        {
            get => _AppEnvironmentProvider;
            set
            {
                _AppEnvironmentProvider = value;
                _App = _AppEnvironmentProvider.GetActiveAppEnvironment();
            }
        }

        private readonly ConcurrentDictionary<string, BasicDataProtector> _basicDataProtectors = new ConcurrentDictionary<string, BasicDataProtector>();

        public virtual IDataProtector Create(string[] purposes)
        {
            if (purposes == null)
                throw new ArgumentNullException(nameof(purposes));

            string appName = _App.AppInfo.Name;

            string key = $"{appName} => {string.Join(",", purposes)}";

            return _basicDataProtectors.GetOrAdd(key, (k) => new BasicDataProtector(appName, purposes));
        }
    }

    public class BasicDataProtector : IDataProtector
    {
        private const string PrimaryPurpose = "Microsoft.Owin.Security.IDataProtector";

        private const DataProtectionScope Scope = DataProtectionScope.CurrentUser;

        private readonly byte[] _entropy;

        public BasicDataProtector(string appName, string[] purposes)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, md5, CryptoStreamMode.Write))
                using (StreamWriter writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(appName);
                    writer.Write(PrimaryPurpose);

                    foreach (string purpose in purposes)
                    {
                        writer.Write(purpose);
                    }
                }
                _entropy = md5.Hash;
            }
        }

        public virtual byte[] Protect(byte[] userData)
        {
            return ProtectedData.Protect(userData, _entropy, Scope);
        }

        public virtual byte[] Unprotect(byte[] protectedData)
        {
            return ProtectedData.Unprotect(protectedData, _entropy, Scope);
        }
    }
}
