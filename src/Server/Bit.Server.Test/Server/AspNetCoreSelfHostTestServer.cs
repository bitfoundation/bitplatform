using Bit.Owin;
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
