using Bit.Http.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Test.Server
{
    public class WebDriverOptions
    {
        public string Uri { get; set; } = default!;

        public Token? Token { get; set; }

        public bool ClientSideTest { get; set; } = true;
    }

    public interface ITestServer : IDisposable
    {
        Task<Token> LoginWithCredentials(string userName, string password, IDictionary<string, string?>? acr_values = null);

        IODataClient BuildODataClient(Token? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null);

        ODataBatch BuildODataBatchClient(Token? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null);

        HttpClient BuildHttpClient(Token? token = null);

        Task<IHubProxy> BuildSignalRClient(Token? token = null, Action<string, dynamic>? onMessageReceived = null);

        TService BuildRefitClient<TService>(Token? token = null);

        void Initialize(string uri);

        string Uri { get; }
    }
}
