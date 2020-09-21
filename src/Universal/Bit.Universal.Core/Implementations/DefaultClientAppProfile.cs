using Bit.Core.Contracts;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Implementations
{
    public class DefaultClientAppProfile : IClientAppProfile
    {
        public virtual Uri? HostUri { get; set; }

        public virtual Uri? OAuthRedirectUri { get; set; }

        public virtual string? AppName { get; set; }

        public virtual string? ODataRoute { get; set; }

        public virtual string? TokenEndpoint { get; set; } = "core/connect/token";

        public virtual string? LoginEndpoint { get; set; } = "InvokeLogin";

        public virtual string? LogoutEndpint { get; set; } = "InvokeLogout";

        public virtual string? SignalrEndpint { get; set; } = "signalr";

        public string? Environment { get; set; } = GetEnvironment();

        static string GetEnvironment()
        {
            bool isDebugMode;

            try
            {
#if UWP
                isDebugMode = Assembly.GetEntryAssembly()!.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
#else
                isDebugMode = Debugger.IsAttached;
#endif
            }
            catch
            {
#if UWP
                isDebugMode = Debugger.IsAttached;
#else
                isDebugMode = false;
#endif
            }

            return isDebugMode ? "Development" : "Production";
        }
    }
}
