using System;

namespace Bit.ViewModel.Contracts
{
    public interface IClientAppProfile
    {
        Uri HostUri { get; set; }

        Uri OAuthRedirectUri { get; set; }

        string AppName { get; set; }

        string ODataRoute { get; set; }
    }
}
