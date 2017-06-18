using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.OData;
using Autofac.Integration.WebApi;
using Bit.Api.Contracts.Project;
using Bit.Api.Implementations.Project;
using Bit.Api.Middlewares.WebApi;
using Bit.Api.Middlewares.WebApi.ActionFilters;
using Bit.Api.Middlewares.WebApi.Contracts;
using Bit.Api.Middlewares.WebApi.Implementations;
using Bit.Api.Middlewares.WebApi.OData;
using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Api.Middlewares.WebApi.OData.Implementations;
using Bit.Owin.Middlewares;

namespace Bit.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterDefaultWebApiConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            controllersAssemblies = (controllersAssemblies.Any() ? controllersAssemblies : new[] { Assembly.GetCallingAssembly(), AssemblyContainer.Current.GetBitApiAssembly() }).Union(new[] { typeof(MetadataController).GetTypeInfo().Assembly }).ToArray();

            dependencyManager.RegisterInstance<IApiAssembliesProvider>(new DefaultApiAssembliesProvider(controllersAssemblies), overwriteExciting: false);

            dependencyManager.RegisterApiControllers(controllersAssemblies);

            dependencyManager.Register<System.Web.Http.Dispatcher.IAssembliesResolver, DefaultWebApiAssembliesResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Tracing.ITraceWriter, DefaultWebApiTraceWritter>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new OwinActionFilterAttribute(typeof(OwinNoCacheResponseMiddleware))));
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalHostAuthenticationFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogActionArgsActionFilterProvider>();
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

        public static IDependencyManager RegisterGlobalWebApiActionFiltersUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> addGlobalActionFilters)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (addGlobalActionFilters == null)
                throw new ArgumentNullException(nameof(addGlobalActionFilters));

            dependencyManager.RegisterInstance<IWebApiConfigurationCustomizer>(new DelegateGlobalActionFiltersProvider(addGlobalActionFilters), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterEdmModelProvider<TEdmModelProvider>(this IDependencyManager dependencyManager)
            where TEdmModelProvider : class, IEdmModelProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IEdmModelProvider, TEdmModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiMiddlewareConfiguration>(name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiODataMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiODataOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalODataNullReturnValueActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<DefaultGlobalEnableQueryActionFilterProvider>();
            dependencyManager.Register<IAutoEdmBuilder, DefaultAutoEdmBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataModelBuilderProvider, DefaultODataModelBuilderProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataContainerBuilderCustomizer, DefaultODataContainerBuilderCustomizer>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Controllers.IHttpActionSelector, DefaultWebApiODataControllerActionSelector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiODataMiddlewareConfiguration>(name);

            return dependencyManager;
        }
    }
}
