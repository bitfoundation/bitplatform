using Autofac;
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

namespace WebApiDependencyInjection
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9007/";

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
            EmailService.SendEmail();

            return new string[] { "value1", "value2" };
        }

        public virtual IEmailService EmailService { get; set; }
    }

    public interface IEmailService
    {
        void SendEmail();
    }

    public class DefaultEmailService : IEmailService
    {
        public virtual void SendEmail()
        {

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
            });

            dependencyManager.Register<IEmailService, DefaultEmailService>(lifeCycle: DependencyLifeCycle.PerScopeInstance);

            ContainerBuilder autofacContainerBuilder = ((IAutofacDependencyManager)dependencyManager).GetContainerBuidler();
        }
    }
}
