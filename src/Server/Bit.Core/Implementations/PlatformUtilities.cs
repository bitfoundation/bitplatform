using System;

namespace Bit.Core.Implementations
{
    public class PlatformUtilities
    {
        private static readonly Lazy<bool> _isRunningOnMono = new Lazy<bool>(() =>
        {
            try
            {
                return Type.GetType("Mono.Runtime") != null;
            }
            catch
            {
                return false;
            }
        });

        private static readonly Lazy<bool> _isRunningOnDotNetCore = new Lazy<bool>(() =>
        {
            try
            {
                return Type.GetType("System.Runtime.Loader.AssemblyLoadContext") != null;
            }
            catch
            {
                return false;
            }
        });

        public static bool IsRunningOnMono
        {
            get
            {
                return _isRunningOnMono.Value;
            }
        }

        public static bool IsRunningOnDotNetCore
        {
            get
            {
                return _isRunningOnDotNetCore.Value;
            }
        }
    }
}
