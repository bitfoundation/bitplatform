using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Host.HttpListener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.AspNetCore.Implementations.Servers
{
    public class HttpListenerServer : IServer
    {
        private IDisposable _HttpListenerServer;

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public HttpListenerServer()
        {
            Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            Func<IDictionary<string, object>, Task> appFunc = async env =>
            {
                FeatureCollection features = new FeatureCollection(new OwinFeatureCollection(env));

                TContext context = application.CreateContext(features);

                try
                {
                    await application.ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    application.DisposeContext(context, ex);
                    throw;
                }

                application.DisposeContext(context, null);

            };

            appFunc = OwinWebSocketAcceptAdapter.AdaptWebSockets(appFunc);

            Dictionary<string, object> props = new Dictionary<string, object>();

            props["host.Addresses"] = Features.Get<IServerAddressesFeature>().Addresses.Select(add => new Uri(add)).Select(add => new Address(add.Scheme, add.Host, add.Port.ToString(), add.LocalPath).Dictionary).ToList();

            OwinServerFactory.Initialize(props);

            _HttpListenerServer = OwinServerFactory.Create(appFunc, props);
        }

        public void Dispose()
        {
            _HttpListenerServer?.Dispose();
        }
    }

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
