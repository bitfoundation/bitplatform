using Bit.Core.Contracts;
using Bit.ViewModel.Implementations;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using System.Net.Http;

namespace Bit.ViewModel.Contracts
{
    public delegate SignalRHttpClient ISignalRHttpClientFactory(HttpMessageHandler httpMessageHandler);
    public delegate HubConnection IHubConnectionFactory(IClientAppProfile clientAppProfile);
    public delegate DefaultServerSentEventsTransport IClientTransportFactory(IHttpClient signalRHttpClient);
}
