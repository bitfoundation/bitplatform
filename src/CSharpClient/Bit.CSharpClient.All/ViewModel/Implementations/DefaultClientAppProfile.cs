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
    }
}
