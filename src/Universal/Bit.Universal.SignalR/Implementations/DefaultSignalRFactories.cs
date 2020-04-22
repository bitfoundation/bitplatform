using Bit.Core.Contracts;
using Bit.ViewModel.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using System;
using System.Net.Http;

namespace Bit.ViewModel.Implementations
{
    public static class DefaultSignalRFactories
    {
        public static SignalRHttpClient SignalRHttpClientFactory(HttpMessageHandler httpMessageHandler)
        {
            if (httpMessageHandler == null)
                throw new ArgumentNullException(nameof(httpMessageHandler));

            return new SignalRHttpClient(httpMessageHandler);
        }

        public static HubConnection IHubConnectionFactory(IClientAppProfile clientAppProfile)
        {
            if (clientAppProfile == null)
                throw new ArgumentNullException(nameof(clientAppProfile));

            return new HubConnection($"{clientAppProfile.HostUri ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.HostUri)} is null")}{clientAppProfile.SignalrEndpint ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.SignalrEndpint)} is null")}")
            {
                TransportConnectTimeout = TimeSpan.FromSeconds(3)
            };
        }

        public static DefaultServerSentEventsTransport IClientTransportFactory(IHttpClient signalRHttpClient)
        {
            if (signalRHttpClient == null)
                throw new ArgumentNullException(nameof(signalRHttpClient));

            return new DefaultServerSentEventsTransport(signalRHttpClient)
            {
                ReconnectDelay = TimeSpan.FromSeconds(3)
            };
        }
    }
}
