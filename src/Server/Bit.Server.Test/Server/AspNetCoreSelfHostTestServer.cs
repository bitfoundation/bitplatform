using Bit.Owin;
using Bit.Test.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;

namespace Bit.Test.Server
{
    public class AspNetCoreSelfHostTestServer : TestServerBase
    {
        private IHost? _host;
        private TestEnvironmentArgs _args;

        public AspNetCoreSelfHostTestServer(TestEnvironmentArgs args)
        {
            _args = args;
        }

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _host = BitWebHost.CreateWebHost(Array.Empty<string>())
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseUrls(uri);
                })
                .Start();
        }

        protected override void Dispose(bool disposing)
        {
            _host?.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            _host?.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return new TestHttpClientHandler(new HttpClientHandler(), _args);
        }
    }
}
