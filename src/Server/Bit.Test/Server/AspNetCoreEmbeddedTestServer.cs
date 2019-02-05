using System.IO;
using System.Net.Http;
using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using OpenQA.Selenium.Remote;
using System;

namespace Bit.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer _server;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _server = new TestServer(BitWebHost.CreateDefaultBuilder(new string[] { })
                .UseUrls(uri)
                .UseStartup<AutofacAspNetCoreAppStartup>());
        }

        public override void Dispose()
        {
            _server.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return _server.CreateHandler();
        }

        public override RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions options = null)
        {
            throw new NotSupportedException();
        }
    }
}
