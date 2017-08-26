using Autofac;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
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

    public class AppStartup : OwinAppStartup, IOwinDependenciesManager, IDependenciesManagerProvider
    {
        public override void Configuration(IAppBuilder owinApp)
        {
            DefaultDependenciesManagerProvider.Current = this;

            base.Configuration(owinApp);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public void ConfigureDependencies(IDependencyManager dependencyManager)
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

            dependencyManager.Register<IEmailService, DefaultEmailService>(lifeCycle: DependencyLifeCycle.InstancePerLifetimeScope);

            ContainerBuilder autofacContainerBuilder = ((IAutofacDependencyManager)dependencyManager).GetContainerBuidler();
        }
    }
}
