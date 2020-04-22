using System;
using System.Net.Http;
using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Test.Server
{
    public class AspNetCoreSelfHostTestServer : TestServerBase
    {
        private IWebHost? _host;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _host = BitWebHost.CreateDefaultBuilder(Array.Empty<string>())
                .UseUrls(uri)
                .UseStartup<AutofacAspNetCoreAppStartup>()
                .Build();

            _host.Start();
        }

        public override void Dispose()
        {
            _host?.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            _host?.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return new HttpClientHandler();
        }
    }
}
