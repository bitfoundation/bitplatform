using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Hangfire.Implementations;
using Bit.Model.Implementations;
using Bit.OData.ActionFilters;
using Bit.OData.Implementations;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.Signalr.Implementations;
using BitChangeSetManager.Api.Implementations;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Security;
using Microsoft.Owin.Cors;
using Owin;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace BitChangeSetManager
{
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

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo());

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();

            dependencyManager.RegisterAppEvents<BitChangeSetManagerInitialData>();
            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterOwinMiddlewareUsing(owinApp =>
            {
                owinApp.UseCors(CorsOptions.AllowAll);
            });

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterMinimalOwinMiddlewares();
            dependencyManager.RegisterSingleSignOnClient();
            dependencyManager.RegisterMetadata();

            dependencyManager.RegisterDefaultWebApiAndODataConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new System.Web.Http.AuthorizeAttribute());
                });

                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.RegisterODataMiddleware(childDependencyManager =>
            {
                childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                });

                childDependencyManager.RegisterEdmModelProvider<BitEdmModelProvider>();
                childDependencyManager.RegisterEdmModelProvider<BitChangeSetManagerEdmModelProvider>();
                childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
            });

            if (DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().DebugMode == false)
                dependencyManager.RegisterSignalRConfiguration<SignalRSqlServerScaleoutConfiguration>();
            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration<BitChangeSetManagerAppMessageHubEvents>();

            dependencyManager.RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.RegisterGeneric(typeof(IBitChangeSetManagerRepository<>).GetTypeInfo(), typeof(BitChangeSetManagerEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterEfDbContext<BitChangeSetManagerDbContext>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<BitChangeSetManagerDtoModelMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<BitChangeSetManagerUserService, BitChangeSetManagerClientProvider>();

            dependencyManager.RegisterSecureDefaultPageMiddlewareUsingDefaultConfiguration();

            dependencyManager.Register<IChangeSetRepository, ChangeSetRepository>();
            dependencyManager.Register<IUserSettingProvider, BitUserSettingProvider>();
        }
    }
}
