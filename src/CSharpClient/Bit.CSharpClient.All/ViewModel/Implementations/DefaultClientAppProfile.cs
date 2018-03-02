using Bit.ViewModel.Contracts;
using System;

namespace Bit.ViewModel.Implementations
{
    public class DefaultClientAppProfile : IClientAppProfile
    {
        public Uri HostUri { get; set; }

        public Uri OAuthRedirectUri { get; set; }

        public string AppName { get; set; }
    }
}
