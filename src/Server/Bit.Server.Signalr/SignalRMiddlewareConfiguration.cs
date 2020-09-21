using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Signalr
{
    public class SignalRMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;
        public virtual IEnumerable<ISignalRConfiguration> SignalRConfigurations { get; set; } = default!;
        public virtual Microsoft.AspNet.SignalR.IDependencyResolver DependencyResolver { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            HubConfiguration signalRConfig = new HubConfiguration
            {
                EnableDetailedErrors = AppEnvironment.DebugMode == true,
                EnableJavaScriptProxies = true,
                EnableJSONP = false,
                Resolver = DependencyResolver
            };

            SignalRConfigurations.ToList()
                .ForEach(cnfg =>
                {
                    cnfg.Configure(signalRConfig);
                });

            owinApp.Map("/signalr", innerOwinApp =>
            {
                innerOwinApp.RunSignalR(signalRConfig);
            });
        }
    }
}