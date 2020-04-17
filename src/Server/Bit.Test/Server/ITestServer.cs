using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bit.Test.Server
{
    public class RemoteWebDriverOptions
    {
        public string Uri { get; set; } = default!;

        public TokenResponse? Token { get; set; }

        public bool ClientSideTest { get; set; } = true;
    }

    public interface ITestServer : IDisposable
    {
        RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions? options = null);

        Task<TokenResponse> Login(string userName, string password, string clientId, string secret = "secret", IDictionary<string, string?>? acr_values = null);

        string GetLoginUrl(string? client_id = null, Uri? redirect_uri = null, object? state = null, IDictionary<string, string?>? acr_values = null);

        IODataClient BuildODataClient(TokenResponse? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null);

        ODataBatch BuildODataBatchClient(TokenResponse? token = null, string odataRouteName = "Test", ODataClientSettings? odataClientSettings = null);

        HttpClient BuildHttpClient(TokenResponse? token = null);

        Task<IHubProxy> BuildSignalRClient(TokenResponse? token = null, Action<string, dynamic>? onMessageReceived = null);

        TService BuildRefitClient<TService>(TokenResponse? token = null);

        void Initialize(string uri);

        string Uri { get; }
    }
}
