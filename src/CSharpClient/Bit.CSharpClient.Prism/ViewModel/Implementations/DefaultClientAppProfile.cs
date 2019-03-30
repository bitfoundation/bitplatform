using Bit.ViewModel.Contracts;
using System;

namespace Bit.ViewModel.Implementations
{
    public class DefaultClientAppProfile : IClientAppProfile
    {
        public virtual Uri HostUri { get; set; }

        public virtual Uri OAuthRedirectUri { get; set; }

        public virtual string AppName { get; set; }

        public virtual string ODataRoute { get; set; }

        public virtual string TokenEndpoint { get; set; } = "core/connect/token";

        public virtual string LoginEndpoint { get; set; } = "InvokeLogin";

        public virtual string LogoutEndpint { get; set; } = "InvokeLogout";

        public virtual string SignalrEndpint { get; set; } = "signalr";
    }
}
