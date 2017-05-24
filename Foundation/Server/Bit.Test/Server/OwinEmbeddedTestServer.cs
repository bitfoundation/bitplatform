using System;
using System.Net.Http;
using Foundation.Api;
using Microsoft.Owin.Testing;
using OpenQA.Selenium.Remote;

namespace Foundation.Test.Server
{
    public class OwinEmbeddedTestServer : TestServerBase
    {
        private TestServer _server;

        public override void Dispose()
        {
            _server.Dispose();
        }

        public override void Initialize(string uri)
        {
            base.Initialize(uri);
            _server = TestServer.Create<OwinAppStartup>();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return _server.Handler;
        }

        public override RemoteWebDriver GetWebDriver(RemoteWebDriverOptions options = null)
        {
            throw new NotSupportedException();
        }
    }
}
