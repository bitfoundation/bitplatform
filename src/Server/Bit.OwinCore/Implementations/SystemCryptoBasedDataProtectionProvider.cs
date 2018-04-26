using Bit.Core.Models;
using Microsoft.Owin.Security.DataProtection;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Bit.OwinCore.Implementations
{
    public class SystemCryptoBasedDataProtectionProvider : IDataProtectionProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        private readonly ConcurrentDictionary<string, SystemCryptoBasedDataProtection> _basicDataProtectors = new ConcurrentDictionary<string, SystemCryptoBasedDataProtection>();

        public virtual IDataProtector Create(string[] purposes)
        {
            if (purposes == null)
                throw new ArgumentNullException(nameof(purposes));

            string appName = AppEnvironment.AppInfo.Name;

            string key = $"{appName} => {string.Join(",", purposes)}";

            return _basicDataProtectors.GetOrAdd(key, (k) => new SystemCryptoBasedDataProtection());
        }
    }

    public class SystemCryptoBasedDataProtection : IDataProtector
    {
        public virtual byte[] Protect(byte[] userData)
        {
            return ProtectedData.Protect(userData, null, DataProtectionScope.LocalMachine);
        }

        public virtual byte[] Unprotect(byte[] protectedData)
        {
            return ProtectedData.Unprotect(protectedData, null, DataProtectionScope.LocalMachine);
        }
    }
}
