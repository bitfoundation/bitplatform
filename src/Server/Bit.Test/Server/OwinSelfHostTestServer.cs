using Bit.Owin;
using Microsoft.Owin.Hosting;
using System;
using System.Net.Http;

namespace Bit.Test.Server
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
