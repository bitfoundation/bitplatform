using System.IO;
using System.Net.Http;
using Bit.OwinCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Bit.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer _server;

        public override void Initialize(string uri)
        {
            base.Initialize(uri);

            _server = new TestServer(new WebHostBuilder()
                .UseUrls(uri)
                .UseContentRoot(Directory.GetCurrentDirectory())
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
    }
}
