using Autofac.Integration.WebApi;
using Bit.OData;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Bit.WebApi.Implementations;
using Microsoft.AspNet.OData;
using Microsoft.OData;
using System;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterWebApiODataMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = "WebApiOData")
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiODataOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterWebApiConfigurationCustomizer<ReadRequestContentStreamAsyncActionFilterAttribute>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalODataNullReturnValueActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<SetODataSwaggerActionSelector>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<DefaultGlobalEnableQueryActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogOperationInfoActionFilterProvider<ODataLogOperationInfoFilterAttribute>>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<ThrowAnExceptionForRequestBodyJsonParseErrorActionFilter>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider<ODataExceptionHandlerFilterAttribute>>();
            dependencyManager.Register<IODataModuleConfiguration, DefaultODataModuleConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IContainerBuilder, DefaultODataContainerBuilder>(lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.Register<IODataModelBuilderProvider, DefaultODataModelBuilderProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<System.Web.Http.Controllers.IHttpActionSelector, DefaultWebApiODataControllerActionSelector>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterOwinMiddleware<WebApiODataMiddlewareConfiguration>(name);
            dependencyManager.RegisterODataFactories();

            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultWebApiAndODataConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IODataSqlBuilder, DefaultODataSqlBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            return dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.AssembliesWithDefaultAssemblies(controllersAssemblies).Union(new[] { AssemblyContainer.Current.GetServerWebApiAssembly(), AssemblyContainer.Current.GetServerODataAssembly(), typeof(MetadataController).GetTypeInfo().Assembly }).ToArray());
        }

        public static IDependencyManager RegisterODataMiddleware(this IDependencyManager dependencyManager, Action<IDependencyManager> onConfigure)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterUsing((resolver) => ((IDependencyManager)resolver).CreateChildDependencyResolver(onConfigure).Resolve<IOwinMiddlewareConfiguration>("WebApiOData"), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
