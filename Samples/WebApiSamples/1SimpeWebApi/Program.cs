using Bit.Core;
using Bit.Core.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web.Http;

namespace SimpeWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9001/";

            using (WebApp.Start<AppStartup>(url: baseAddress))
            {
                Process.Start($"{baseAddress}api/values/");
                Console.ReadLine();
            }
        }
    }

    public class ValuesController : ApiController
    {
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }

    public class AppStartup : OwinAppStartup, IAppModule, IAppModulesProvider
    {
        public override void Configuration(IAppBuilder owinApp)
        {
            DefaultAppModulesProvider.Current = this;

            base.Configuration(owinApp);
        }

        public IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }

        public void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterMinimalOwinMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();

                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    // You've access to web api's http configuration here
                });
            });
        }
    }
}
