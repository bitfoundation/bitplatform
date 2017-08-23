using Autofac.Integration.WebApi;
using Bit.OData;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Bit.WebApi.Implementations;
using Microsoft.OData;
using System;
using System.Linq;
using System.Reflection;
using System.Web.OData;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterODataModelBuilder<TODataModelBuilder>(this IDependencyManager dependencyManager)
            where TODataModelBuilder : class, IODataModelBuilder
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IODataModelBuilder, TODataModelBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiODataMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = "WebApiOData")
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiODataOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalODataNullReturnValueActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<DefaultGlobalEnableQueryActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogOperationArgsActionFilterProvider<ODataLogOperationArgsFilterAttribute>>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider<ODataExceptionHandlerFilterAttribute>>();
            dependencyManager.Register<IAutoODataModelBuilder, DefaultAutoODataModelBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IContainerBuilder, ExtendedContainerBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IODataModelBuilderProvider, DefaultODataModelBuilderProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<System.Web.Http.Controllers.IHttpActionSelector, DefaultWebApiODataControllerActionSelector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<WebApiODataMiddlewareConfiguration>(name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultWebApiAndODataConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            dependencyManager.Register<IODataSqlBuilder, DefaultODataSqlBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            return dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.AssembliesWithDefaultAssemblies(controllersAssemblies).Union(new[] { AssemblyContainer.Current.GetBitWebApiAssembly(), AssemblyContainer.Current.GetBitODataAssembly(), typeof(MetadataController).GetTypeInfo().Assembly }).ToArray());
        }

        public static IDependencyManager RegisterODataMiddleware(this IDependencyManager dependencyManager, Action<IDependencyManager> onConfigure)
        {
            dependencyManager.RegisterUsing(() => dependencyManager.CreateChildDependencyResolver(onConfigure).Resolve<IOwinMiddlewareConfiguration>("WebApiOData"), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
