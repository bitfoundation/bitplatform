using System;

namespace Bit.ViewModel.Contracts
{
    public interface IConfigProvider
    {
        Uri HostUri { get; }

        string OAuthImplicitFlowClientId { get; }

        Uri OAuthImplicitFlowRedirectUri { get; }

        string OAuthResourceOwnerFlowClientId { get; }

        string OAuthResourceOwnerFlowSecret { get; }

        string AppName { get; }
    }
}
