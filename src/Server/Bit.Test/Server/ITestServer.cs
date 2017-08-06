using System;
using System.Net.Http;
using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
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
        RemoteWebDriver GetWebDriver(RemoteWebDriverOptions options = null);

        Task<TokenResponse> Login(string userName, string password, string clientId, string secret = "secret");

        ODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string edmName = "Test");

        ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
           Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string edmName = "Test");

        HttpClient GetHttpClient(TokenResponse token = null);

        Task<IHubProxy> BuildSignalRClient(TokenResponse token = null, Action<string, dynamic> onMessageReceived = null);

        TokenClient BuildTokenClient(string clientId, string secret);

        void Initialize(string uri);

        string Uri { get; }
    }
}
