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
            return new SignalRHttpClient(httpMessageHandler);
        }

        public static HubConnection IHubConnectionFactory(IClientAppProfile clientAppProfile)
        {
            return new HubConnection($"{clientAppProfile.HostUri}{clientAppProfile.SignalrEndpint}")
            {
                TransportConnectTimeout = TimeSpan.FromSeconds(3)
            };
        }

        public static DefaultServerSentEventsTransport IClientTransportFactory(IHttpClient signalRHttpClient)
        {
            return new DefaultServerSentEventsTransport(signalRHttpClient)
            {
                ReconnectDelay = TimeSpan.FromSeconds(3)
            };
        }
    }
}
