using System;

namespace Bit.ViewModel.Contracts
{
    public interface IClientAppProfile
    {
        Uri HostUri { get; set; }

        Uri OAuthRedirectUri { get; set; }

        string AppName { get; set; }

        string ODataRoute { get; set; }

        string TokenEndpoint { get; set; }

        string LoginEndpoint { get; set; }

        string LogoutEndpint { get; set; }

        string SignalrEndpint { get; set; }
    }
}
