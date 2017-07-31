using System.IO;
using System.Net.Http;
using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;

namespace Bit.Test.Server
{
    public class AspNetCoreSelfHostTestServer : TestServerBase
    {
        private IWebHost _host;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _host = new WebHostBuilder()
                .UseHttpListener()
                .UseUrls(uri)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<AutofacAspNetCoreAppStartup>()
                .CaptureStartupErrors(true)
                .Build();

            _host.Start();
        }

        public override void Dispose()
        {
            _host.Dispose();
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return new HttpClientHandler();
        }
    }
}
