using Bit.OwinCore.Implementations.Servers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Host.HttpListener;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.OwinCore.Implementations.Servers
{
    public class HttpListenerServer : OwinServer, IServer
    {
        private IDisposable _HttpListenerServer;

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            CreateOwinProps(application, out Func<IDictionary<string, object>, Task> appFunc, out Dictionary<string, object> props);

            OwinServerFactory.Initialize(props);

            _HttpListenerServer = OwinServerFactory.Create(appFunc, props);
        }

        public void Dispose()
        {
            _HttpListenerServer?.Dispose();
        }
    }
}

namespace Microsoft.AspNetCore.Hosting
{
    public static class HttpListenerWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseHttpListener(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServer, HttpListenerServer>();
            });
        }
    }
}