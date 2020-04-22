using System.Net.Http;
using Bit.Owin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using OpenQA.Selenium.Remote;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer? _server;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _server = new TestServer(BitWebHost.CreateDefaultBuilder(Array.Empty<string>())
                .UseUrls(uri)
                .UseStartup<AutofacAspNetCoreAppStartup>());
        }

        public override void Dispose()
        {
            _server?.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            _server?.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return _server!.CreateHandler();
        }

        public override RemoteWebDriver BuildWebDriver(RemoteWebDriverOptions? options = null)
        {
            throw new NotSupportedException();
        }
    }
}
