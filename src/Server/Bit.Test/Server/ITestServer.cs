using System;
using System.Net.Http;
using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using OpenQA.Selenium.Remote;

namespace Bit.Test.Server
{
    public class RemoteWebDriverOptions
    {
        public string Uri { get; set; } = null;

        public TokenResponse Token { get; set; } = null;

        public bool ClientSideTest { get; set; } = true;
    }

    public interface ITestServer : IDisposable
    {
        RemoteWebDriver GetWebDriver(RemoteWebDriverOptions options = null);

        TokenResponse Login(string userName, string password, string clientName, string secret = "secret");

        /*ODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string route = null);*/

        /*ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
           Action<HttpResponseMessage> afterResponse = null, TokenResponse token = null, string route = null);*/

        HttpClient GetHttpClient(TokenResponse token = null);

        IHubProxy BuildSignalRClient(TokenResponse token = null, Action<string, dynamic> onMessageReceived = null);

        TokenClient BuildTokenClient(string clientName, string secret);

        void Initialize(string uri);

        string Uri { get; }
    }
}
