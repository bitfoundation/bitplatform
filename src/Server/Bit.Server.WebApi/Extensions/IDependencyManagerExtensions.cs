using Autofac.Integration.WebApi;
using Bit.Owin.Contracts;
using Bit.Owin.Middlewares;
using Bit.WebApi;
using Bit.WebApi.ActionFilters;
using Bit.WebApi.Contracts;
using Bit.WebApi.Implementations;
using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        /// <summary>
        /// Configures web api. It finds web api controllers in <see cref="AssemblyContainer"/> app assemblies. and it registers <see cref="IApiAssembliesProvider"/> by <see cref="DefaultWebApiAssembliesProvider"/>
        /// | <see cref="System.Web.Http.Dispatcher.IAssembliesResolver"/> by <see cref="DefaultWebApiAssembliesResolver"/>
        /// | <see cref="System.Web.Http.Tracing.ITraceWriter"/> by <see cref="DefaultWebApiTraceWriter"/>
        /// | It adds <see cref="OwinNoCacheResponseMiddleware"/> middleware and <see cref="AddAcceptCharsetToRequestHeadersIfNotAnyFilterAttribute"/> action filter globally
        /// | It configures web api by <see cref="GlobalHostAuthenticationFilterProvider"/> and <see cref="XCorrelationWebApiConfigurationCustomizer"/>
        /// </summary>
        /// <param name="controllersAssemblies">Bit finds web api controllers in these assemblies too</param>
        public static IDependencyManager RegisterDefaultWebApiConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (controllersAssemblies == null)
                throw new ArgumentNullException(nameof(controllersAssemblies));

            controllersAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(controllersAssemblies).Union(new[] { AssemblyContainer.Current.GetServerWebApiAssembly() }).ToArray();

            dependencyManager.RegisterInstance<IApiAssembliesProvider>(new DefaultWebApiAssembliesProvider(controllersAssemblies), overwriteExisting: false);

            dependencyManager.RegisterAssemblyTypes(controllersAssemblies, t => typeof(IHttpController).GetTypeInfo().IsAssignableFrom(t) && t.Name.EndsWith("Controller", StringComparison.InvariantCultureIgnoreCase), lifeCycle: DependencyLifeCycle.Transient);

            dependencyManager.Register<System.Web.Http.Dispatcher.IAssembliesResolver, DefaultWebApiAssembliesResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<System.Web.Http.Tracing.ITraceWriter, DefaultWebApiTraceWriter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new OwinActionFilterAttribute(typeof(OwinNoCacheResponseMiddleware))));
            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new AddAcceptCharsetToRequestHeadersIfNotAnyFilterAttribute()));
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalHostAuthenticationFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<XCorrelationWebApiConfigurationCustomizer>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiConfigurationCustomizer<TWebApiConfigurationCustomizer>(this IDependencyManager dependencyManager)
            where TWebApiConfigurationCustomizer : class, IWebApiConfigurationCustomizer
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IWebApiConfigurationCustomizer, TWebApiConfigurationCustomizer>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }

        /// <summary>
        /// Using this method you can customize web api's http configuration
        /// </summary>
        /// <param name="webApiCustomizer">http configuration to be customized</param>
        public static IDependencyManager RegisterGlobalWebApiCustomizerUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> webApiCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (webApiCustomizer == null)
                throw new ArgumentNullException(nameof(webApiCustomizer));

            dependencyManager.RegisterInstance<IWebApiConfigurationCustomizer>(new DelegateGlobalActionFiltersProvider(webApiCustomizer), overwriteExisting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiActionFiltersUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> addGlobalActionFilters)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return dependencyManager.RegisterGlobalWebApiCustomizerUsing(addGlobalActionFilters);
        }

        /// <summary>
        /// Adds minimal dependencies to make web api work. It registers <see cref="System.Web.Http.Dependencies.IDependencyResolver"/> by <see cref="AutofacWebApiDependencyResolver"/>
        /// | <see cref="IWebApiOwinPipelineInjector"/> by <see cref="DefaultWebApiOwinPipelineInjector"/>
        /// It adds <see cref="LogOperationInfoFilterAttribute"/> and <see cref="ExceptionHandlerFilterAttribute"/> action filters
        /// It registers <see cref="WebApiMiddlewareConfiguration"/> middleware
        /// </summary>
        public static IDependencyManager RegisterWebApiMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = "WebApi")
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogOperationInfoActionFilterProvider<LogOperationInfoFilterAttribute>>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider<ExceptionHandlerFilterAttribute>>();
            dependencyManager.RegisterOwinMiddleware<WebApiMiddlewareConfiguration>(name);
            dependencyManager.RegisterWebApiFactories();

            return dependencyManager;
        }

        /// <summary>
        /// Adds WebApi middleware
        /// </summary>
        /// <param name="onConfigure">Everything you perform using this dependency manager, will be applied to this web api only. You can provide any implementation for web api | bit interfaces such as <see cref="System.Web.Http.Tracing.ITraceWriter"/> that affects this web api only.</param>
        public static IDependencyManager RegisterWebApiMiddleware(this IDependencyManager dependencyManager, Action<IDependencyManager>? onConfigure)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterUsing((resolver) => dependencyManager.CreateChildDependencyResolver(onConfigure).Resolve<IOwinMiddlewareConfiguration>("WebApi"), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
