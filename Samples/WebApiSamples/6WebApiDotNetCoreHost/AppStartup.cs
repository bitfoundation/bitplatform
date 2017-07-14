using Bit.Core;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.OwinCore;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Reflection;

namespace WebApiDotNetCoreHost
{
    public class AppStartup : AutofacAspNetCoreAppStartup, IAspNetCoreDependenciesManager, IDependenciesManagerProvider
    {
        public AppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DefaultDependenciesManagerProvider.Current = this;

            return base.ConfigureServices(services);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager)
        {
AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultOwinCoreApp();

            dependencyManager.RegisterMinimalOwinCoreMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });
        }
    }

    public class ValuesController : ApiController
    {
        // GET api/values 
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5 
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values 
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
        }
    }
}
