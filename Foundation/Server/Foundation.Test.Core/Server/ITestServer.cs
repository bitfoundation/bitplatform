using IdentityModel.Client;
using Microsoft.AspNet.SignalR.Client;
using OpenQA.Selenium.Remote;
using Simple.OData.Client;
using System;
using System.Net.Http;

namespace Foundation.Test
{
    public class OAuthToken
    {
        public virtual string token_type { get; set; }

        public virtual string access_token { get; set; }
    }

    public class RemoteWebDriverOptions
    {
        public string Uri { get; set; } = null;

        public OAuthToken Token { get; set; } = null;

        public bool ClientSideTest { get; set; } = true;
    }

    public interface ITestServer : IDisposable
    {
        RemoteWebDriver GetWebDriver(RemoteWebDriverOptions options = null);

        OAuthToken Login(string userName, string password);

        /*ODataClient BuildODataClient(Action<HttpRequestMessage> beforeRequest = null,
            Action<HttpResponseMessage> afterResponse = null, OAuthToken token = null, string route = null);*/

        /*ODataBatch BuildODataBatchClient(Action<HttpRequestMessage> beforeRequest = null,
           Action<HttpResponseMessage> afterResponse = null, OAuthToken token = null, string route = null);*/

        HttpClient GetHttpClient(OAuthToken token = null);

        IHubProxy BuildSignalRClient(OAuthToken token = null, Action<string, dynamic> onMessageReceived = null);

        TokenClient BuildTokenClient(string clientName, string secret);

        void Initialize(string uri);

        string Uri { get; }
    }
}
