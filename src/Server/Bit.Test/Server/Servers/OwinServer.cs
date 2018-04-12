using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using Microsoft.Owin.BuilderProperties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.OwinCore.Implementations.Servers
{
    public class OwinServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();

        public OwinServer()
        {
            Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        }

        protected virtual void CreateOwinProps<TContext>(IHttpApplication<TContext> application, out Func<IDictionary<string, object>, Task> appFunc, out Dictionary<string, object> props)
        {
            appFunc = async env =>
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

            props = new Dictionary<string, object>
            {
                ["host.Addresses"] =
                Features.Get<IServerAddressesFeature>()
                    .Addresses.Select(add => new Uri(add))
                    .Select(add => new Address(add.Scheme, add.Host, add.Port.ToString(CultureInfo.InvariantCulture), add.LocalPath).Dictionary)
                    .ToList()
            };
        }
    }
}
