using Foundation.Test.Server;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Foundation.AspNetCore.Test.Server
{
    public class AspNetCoreEmbeddedTestServer : TestServerBase
    {
        private TestServer _server;

        public override void Initialize(Uri uri)
        {
            base.Initialize(uri);

            _server = new TestServer(new WebHostBuilder()
                .UseUrls(uri.ToString())
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
