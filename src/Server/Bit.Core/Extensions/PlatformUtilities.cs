using System;

namespace Bit.Core.Extensions
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

        public static bool IsRunningOnMono => _isRunningOnMono.Value;

        public static bool IsRunningOnDotNetCore { get; } = _isRunningOnDotNetCore.Value;
    }
}
