using Autofac.Integration.SignalR;
using Autofac.Integration.WebApi;
using AutoMapper;
using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Contracts.Project;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.JobScheduler;
using Foundation.Api.Middlewares.JobScheduler.Implementations;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi;
using Foundation.Api.Middlewares.WebApi.ActionFilters;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.Implementations;
using Foundation.Api.Middlewares.WebApi.OData;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using Foundation.Api.Middlewares.WebApi.OData.Implementations;
using Foundation.DataAccess.Contracts.EntityFrameworkCore;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Foundation.Model.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.OData;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterOwinMiddleware<TMiddleware>(this IDependencyManager dependencyManager, string name = null)
            where TMiddleware : class, IOwinMiddlewareConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IOwinMiddlewareConfiguration, TMiddleware>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false, name: name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterAppEvents<TAppEvents>(this IDependencyManager dependencyManager)
    where TAppEvents : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppEvents, TAppEvents>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterOwinMiddlewareUsing(this IDependencyManager dependencyManager, Action<IAppBuilder> owinAppCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (owinAppCustomizer == null)
                throw new ArgumentNullException(nameof(owinAppCustomizer));

            dependencyManager.RegisterInstance<IOwinMiddlewareConfiguration>(new DelegateOwinMiddlewareConfiguration(owinAppCustomizer), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterMinimalDependencies(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterInstance(DefaultDependenciesManagerProvider.Current)
                .RegisterInstance(DefaultDependencyManager.Current);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultPageMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDefaultHtmlPageProvider, RazorDefaultHtmlPageProvider>();
            dependencyManager.Register<IDefaultPageModelProvider, DefaultPageModelProvider>();

            dependencyManager.RegisterAppEvents<DefaultHtmlPageRazorTemplateConfiguration>();
            dependencyManager.RegisterOwinMiddleware<DefaultPageMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration<TMessagesHubEvents>(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
            where TMessagesHubEvents : class, IMessagesHubEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            hubsAssemblies = hubsAssemblies.Any() ? hubsAssemblies : new[] { Assembly.GetCallingAssembly(), typeof(MessagesHub).GetTypeInfo().Assembly };

            dependencyManager.RegisterHubs(hubsAssemblies);
            dependencyManager.Register<IMessagesHubEvents, TMessagesHubEvents>();
            dependencyManager.Register<IMessageSender, SignalRMessageSender>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IMessageContentFormatter, SignalRMessageContentFormatter>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<Microsoft.AspNet.SignalR.IDependencyResolver, AutofacDependencyResolver>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterInstance<Microsoft.AspNet.SignalR.Hubs.IAssemblyLocator>(new DefaultSignalRAssemblyLocator(hubsAssemblies));

            dependencyManager.RegisterOwinMiddleware<SignalRMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
        {
            return RegisterSignalRMiddlewareUsingDefaultConfiguration<DefaultMessageHubEvents>(dependencyManager, hubsAssemblies);
        }

        public static IDependencyManager RegisterBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerInMemoryBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerInMemoryBackendConfiguration : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDashboardAuthorizationFilter, DefaultJobsDashboardAuthorizationFilter>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<JobSchedulerMiddlewareConfiguration>();
            dependencyManager.RegisterAppEvents<TJobSchedulerInMemoryBackendConfiguration>();
            dependencyManager.Register<IBackgroundJobWorker, DefaultBackgroundJobWorker>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<JobActivator, AutofacJobActivator>(lifeCycle: DepepdencyLifeCycle.SingleInstance);


            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultWebApiConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            controllersAssemblies = (controllersAssemblies.Any() ? controllersAssemblies : new[] { Assembly.GetCallingAssembly(), typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly }).Union(new[] { typeof(MetadataController).GetTypeInfo().Assembly }).ToArray();

            dependencyManager.RegisterInstance<IApiAssembliesProvider>(new DefaultApiAssembliesProvider(controllersAssemblies), overwriteExciting: false);

            dependencyManager.RegisterApiControllers(controllersAssemblies);

            dependencyManager.Register<System.Web.Http.Dispatcher.IAssembliesResolver, DefaultWebApiAssembliesResolver>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Tracing.ITraceWriter, DefaultWebApiTraceWritter>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new OwinActionFilterAttribute(typeof(OwinNoCacheResponseMiddleware))));
            dependencyManager.RegisterGlobalWebApiActionFilter<GlobalHostAuthenticationFilterProvider>();
            dependencyManager.RegisterGlobalWebApiActionFilter<GlobalDefaultExceptionHandlerActionFilterProvider>();
            dependencyManager.RegisterGlobalWebApiActionFilter<GlobalDefaultLogActionArgsActionFilterProvider>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiActionFilter<TActionFilter>(this IDependencyManager dependencyManager)
    where TActionFilter : class, IWebApiGlobalActionFiltersProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IWebApiGlobalActionFiltersProvider, TActionFilter>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiActionFiltersUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> addGlobalActionFilters)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (addGlobalActionFilters == null)
                throw new ArgumentNullException(nameof(addGlobalActionFilters));

            dependencyManager.RegisterInstance<IWebApiGlobalActionFiltersProvider>(new DelegateGlobalActionFiltersProvider(addGlobalActionFilters), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterEdmModelProvider<TEdmModelProvider>(this IDependencyManager dependencyManager)
            where TEdmModelProvider : class, IEdmModelProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IEdmModelProvider, TEdmModelProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiOwinPipelineInjector>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiMiddlewareConfiguration>(name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRConfiguration<TSignalRConfiguration>(this IDependencyManager dependencyManager)
            where TSignalRConfiguration : class, ISignalRConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<ISignalRConfiguration, TSignalRConfiguration>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiODataMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiODataOwinPipelineInjector>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterGlobalWebApiActionFilter<GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider>();
            dependencyManager.RegisterGlobalWebApiActionFilter<GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider>();
            dependencyManager.RegisterGlobalWebApiActionFilter<DefaultGlobalEnableQueryActionFilterProvider>();
            dependencyManager.Register<IAutoEdmBuilder, DefaultAutoEdmBuilder>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataModelBuilderProvider, DefaultODataModelBuilderProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataContainerBuilderCustomizer, DefaultODataContainerBuilderCustomizer>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Controllers.IHttpActionSelector, DefaultWebApiODataControllerActionSelector>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiODataMiddlewareConfiguration>(name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterMetadata(this IDependencyManager dependencyManager, params Assembly[] metadataAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (metadataAssemblies == null)
                throw new ArgumentNullException(nameof(metadataAssemblies));

            metadataAssemblies = metadataAssemblies.Any() ? metadataAssemblies : new[] { Assembly.GetCallingAssembly() };

            metadataAssemblies.SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.IsPublic)
                .Where(t => typeof(IMetadataBuilder).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
                .ToList()
                .ForEach(t =>
                {
                    dependencyManager.Register(typeof(IMetadataBuilder).GetTypeInfo(), t.GetTypeInfo(), lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);
                });

            return dependencyManager;
        }

        public static IDependencyManager RegisterEfCoreAutoMapper(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterGeneric(typeof(IDtoModelMapper<,,>).GetTypeInfo(), typeof(EfCoreDtoModelMapper<,,>).GetTypeInfo(), DepepdencyLifeCycle.SingleInstance);

            dependencyManager.RegisterUsing(() =>
            {
                IEnumerable<IDtoModelMapperConfiguration> configs = dependencyManager.Resolve<IEnumerable<IDtoModelMapperConfiguration>>();

                MapperConfiguration mapperConfig = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMissingTypeMaps = true;
                    configs.ToList().ForEach(c => c.Configure(cfg));
                });

                IMapper mapper = mapperConfig.CreateMapper();

                return mapper;

            }, lifeCycle: DepepdencyLifeCycle.SingleInstance);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDtoModelMapperConfiguration<TDtoModelMapperConfiguration>(this IDependencyManager dependencyManager)
    where TDtoModelMapperConfiguration : class, IDtoModelMapperConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDtoModelMapperConfiguration, TDtoModelMapperConfiguration>(lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterDbContext<TDbContext, TDbContextObjectsProvider>(this IDependencyManager dependencyManager)
            where TDbContext : class
            where TDbContextObjectsProvider : class
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register(typeof(IDbContextObjectsProvider).GetTypeInfo(), typeof(TDbContextObjectsProvider).GetTypeInfo());
            dependencyManager.Register<TDbContext, TDbContext>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterSingleSignOnServer(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterOwinMiddleware<SingleSignOnMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignOutPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLogOutMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLoginMiddlewareConfiguration>();
            return dependencyManager;
        }
    }
}
