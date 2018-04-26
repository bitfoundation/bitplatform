using Bit.OwinCore.Implementations.Servers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Host.HttpListener;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.OwinCore.Implementations.Servers
{
    public class HttpListenerServer : OwinServer, IServer
    {
        private IDisposable _httpListenerServer;

        public virtual Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            CreateOwinProps(application, out Func<IDictionary<string, object>, Task> appFunc, out Dictionary<string, object> props);

            OwinServerFactory.Initialize(props);

            _httpListenerServer = OwinServerFactory.Create(appFunc, props);

            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _httpListenerServer?.Dispose();
            GC.SuppressFinalize(this);
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