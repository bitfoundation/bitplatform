using Foundation.AspNetCore.Implementations.Servers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.BuilderProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.AspNetCore.Implementations.Servers
{
    public class SuaveServer : IServer
    {
        private IDisposable _suaveServer;

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public SuaveServer()
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

            Dictionary<string, object> props = new Dictionary<string, object>
            {
                ["host.Addresses"] =
                Features.Get<IServerAddressesFeature>()
                    .Addresses.Select(add => new Uri(add))
                    .Select(add => new Address(add.Scheme, add.Host, add.Port.ToString(), add.LocalPath).Dictionary)
                    .ToList()
            };


            Suave.Owin.OwinServerFactory.Initialize(props);

            _suaveServer = Suave.Owin.OwinServerFactory.Create(appFunc, props);
        }

        public void Dispose()
        {
            _suaveServer?.Dispose();
        }
    }
}

namespace Microsoft.AspNetCore.Hosting
{
    public static class SuaveWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseSuave(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServer, SuaveServer>();
            });
        }
    }
}
