using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using System.Threading;
using Foundation.Api;
using Microsoft.Owin.Hosting;

namespace Foundation.Test.Server
{
    public class OwinSelfHostTestServer : TestServerBase
    {
        private IDisposable _server;

        public override void Dispose()
        {
            _server.Dispose();
        }

        public override void Initialize(string uri)
        {
            base.Initialize(uri);
            _server = WebApp.Start<OwinAppStartup>(uri);
        }

        protected override HttpMessageHandler GetHttpMessageHandler()
        {
            return new HttpClientHandler();
        }
    }
}
