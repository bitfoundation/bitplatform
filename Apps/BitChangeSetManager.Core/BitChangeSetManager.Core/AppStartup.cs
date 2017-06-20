using Bit.Api.Implementations.Project;
using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Api.Middlewares.WebApi.OData.Implementations;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Core.Implementations;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Hangfire.Middlewares.JobScheduler.Implementations;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.IdentityServer.Middlewares;
using Bit.Model.Implementations;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Bit.OwinCore;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Bit.Signalr.Middlewares.Signalr.Implementations;
using BitChangeSetManager.Api.Implementations;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Security;
using IdentityServer3.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Cors;
using Owin;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BitChangeSetManager.Core
{
    public class AppStartup : AutofacAspNetCoreAppStartup, IDependenciesManager, IDependenciesManagerProvider
    {
        public AppStartup(IHostingEnvironment hostingEnvironment)
            : base(hostingEnvironment)
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

        public override void Configure(IApplicationBuilder aspNetCoreApp, OwinAppStartup owinAppStartup, IEnumerable<IAspNetCoreMiddlewareConfiguration> aspNetCoreMiddlewares)
        {
            aspNetCoreApp.Map("/bit-change-set-manager", innerApsNetCoreApp =>
            {
                base.Configure(innerApsNetCoreApp, owinAppStartup, aspNetCoreMiddlewares);
            });
        }

        public void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>();
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>();
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>();
            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>();
            dependencyManager.Register<IDbConnectionProvider, DefaultSqlDbConnectionProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<BitChangeSetManagerInitialData>();
            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>(); //@Important

            dependencyManager.RegisterOwinMiddlewareUsing(owinApp =>
            {
                owinApp.UseCors(CorsOptions.AllowAll);
            });

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ExtendAspNetCoreAutofacLifetimeToOwinMiddlewareConfiguration>(); //@Important
            // dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>(); @Important
            // dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>(); @Important
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterSingleSignOnServer();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.GetBitApiAssembly(), AssemblyContainer.Current.GetBitChangeSetManagerApiAssembly());

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new System.Web.Http.AuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration("WebApi");

                }).Resolve<IOwinMiddlewareConfiguration>("WebApi");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<BitEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<BitChangeSetManagerEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IODataSqlBuilder, DefaultODataSqlBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance);

            if (DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().DebugMode == false)
                dependencyManager.RegisterSignalRConfiguration<SignalRSqlServerScaleoutConfiguration>();
            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration<BitChangeSetManagerAppMessageHubEvents>(AssemblyContainer.Current.GetBitSignalRAssembly(), AssemblyContainer.Current.GetBitChangeSetManagerSignalrAssembly());

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(AssemblyContainer.Current.GetBitMetadataAssembly(), AssemblyContainer.Current.GetBitChangeSetManagerMetadataAssembly());

            dependencyManager.RegisterGeneric(typeof(IBitChangeSetManagerRepository<>).GetTypeInfo(), typeof(BitChangeSetManagerEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterEfDbContext<BitChangeSetManagerDbContext>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<BitChangeSetManagerDtoModelMapperConfiguration>();

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>();
            dependencyManager.Register<IViewService, DefaultViewService>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISsoPageHtmlProvider, RazorSsoHtmlPageProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISSOPageModelProvider, DefaultSSOPageModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRedirectUriValidator, PatternBasedRedirectUriValidator>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.Register<IClientProvider, BitChangeSetManagerClientProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IUserService, BitChangeSetManagerUserService>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();

            dependencyManager.Register<IChangeSetRepository, ChangeSetRepository>();
        }
    }
}
