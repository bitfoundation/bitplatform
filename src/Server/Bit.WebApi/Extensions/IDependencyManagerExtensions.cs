using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Bit.Owin.Middlewares;
using Autofac.Integration.WebApi;
using Bit.WebApi;
using Bit.WebApi.ActionFilters;
using Bit.WebApi.Contracts;
using Bit.WebApi.Implementations;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterDefaultWebApiConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            controllersAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(controllersAssemblies).Union(new[] { AssemblyContainer.Current.GetBitWebApiAssembly() }).ToArray();

            dependencyManager.RegisterInstance<IApiAssembliesProvider>(new DefaultApiAssembliesProvider(controllersAssemblies), overwriteExciting: false);

            dependencyManager.RegisterApiControllers(controllersAssemblies);

            dependencyManager.Register<System.Web.Http.Dispatcher.IAssembliesResolver, DefaultWebApiAssembliesResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<System.Web.Http.Tracing.ITraceWriter, DefaultWebApiTraceWritter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new OwinActionFilterAttribute(typeof(OwinNoCacheResponseMiddleware))));
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalHostAuthenticationFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<ClientCorrelationWebApiConfigurationCustomizer>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiConfigurationCustomizer<TActionFilter>(this IDependencyManager dependencyManager)
            where TActionFilter : class, IWebApiConfigurationCustomizer
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IWebApiConfigurationCustomizer, TActionFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiCustomizerUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> webApiCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (webApiCustomizer == null)
                throw new ArgumentNullException(nameof(webApiCustomizer));

            dependencyManager.RegisterInstance<IWebApiConfigurationCustomizer>(new DelegateGlobalActionFiltersProvider(webApiCustomizer), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiActionFiltersUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> addGlobalActionFilters)
        {
            return dependencyManager.RegisterGlobalWebApiCustomizerUsing(addGlobalActionFilters);
        }

        public static IDependencyManager RegisterWebApiMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogActionArgsActionFilterProvider<LogActionArgsFilterAttribute>>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider<ExceptionHandlerFilterAttribute>>();
            dependencyManager.RegisterOwinMiddleware<WebApiMiddlewareConfiguration>(name);

            return dependencyManager;
        }
    }
}
