using System;

namespace Bit.ViewModel.Contracts
{
    public interface IClientAppProfile
    {
        Uri HostUri { get; }

        Uri OAuthRedirectUri { get; }

        string AppName { get; }
    }
}
