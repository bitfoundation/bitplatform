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
        public string Uri { get; set; }

        public TokenResponse Token { get; set; }

        public bool ClientSideTest { get; set; } = true;
    }

    public interface ITestServer : IDisposable
    {
        RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions options = null);

        /// <summary>
        /// Sample for optional parameters: { "acr_values", "idp:X tenant:Y" }
        /// </summary>
        Task<TokenResponse> Login(string userName, string password, string clientId, string secret = "secret", IDictionary<string, string> parameters = null);

        IODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string odataRouteName = "Test");

        ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
           Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string odataRouteName = "Test");

        HttpClient BuildHttpClient(TokenResponse token = null);

        Task<IHubProxy> BuildSignalRClient(TokenResponse token = null, Action<string, dynamic> onMessageReceived = null);

        TService BuildRefitClient<TService>(TokenResponse token = null);

        void Initialize(string uri);

        string Uri { get; }
    }
}
