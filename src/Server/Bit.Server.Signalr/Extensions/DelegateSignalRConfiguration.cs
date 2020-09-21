using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using System;

namespace Bit.Core.Contracts
{
    public class DelegateSignalRConfiguration : ISignalRConfiguration
    {
        private readonly Action<HubConfiguration> _signalrHubCustomizer;

        public DelegateSignalRConfiguration(Action<HubConfiguration> signalrHubCustomizer)
        {
            if (signalrHubCustomizer == null)
                throw new ArgumentNullException(nameof(signalrHubCustomizer));

            _signalrHubCustomizer = signalrHubCustomizer;
        }

        public virtual void Configure(HubConfiguration signalRConfig)
        {
            _signalrHubCustomizer(signalRConfig);
        }
    }
}