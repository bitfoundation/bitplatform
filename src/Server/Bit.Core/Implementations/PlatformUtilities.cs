using System;

namespace Bit.Core.Implementations
{
    public class PlatformUtilities
    {
        private static readonly Lazy<bool> _isRunningMono = new Lazy<bool>(() =>
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

        public static bool IsRunningOnMono
        {
            get
            {
                return _isRunningMono.Value;
            }
        }

        public static bool IsRunningOnDotNetCore
        {
            get
            {
                return false;
            }
        }
    }
}
