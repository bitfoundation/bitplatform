using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Hangfire.Implementations;
using Bit.Model.Implementations;
using Bit.OData.ActionFilters;
using Bit.OData.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.Signalr.Implementations;
using BitChangeSetManager.Api.Implementations;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.DataAccess.Implementations;
using BitChangeSetManager.Security;
using Microsoft.Owin.Cors;
using Owin;
using Swashbuckle.Application;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

[assembly: ODataModule("BitChangeSetManager")]

namespace BitChangeSetManager
{
    public class AppStartup : OwinAppStartup, IOwinAppModule, IAppModulesProvider
    {
        public override void Configuration(IAppBuilder owinApp)
        {
            DefaultAppModulesProvider.Current = this;

            base.Configuration(owinApp);
        }

        public IEnumerable<IAppModule> GetAppModules()
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

                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        EnvironmentAppInfo appInfo = DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().AppInfo;
                        c.SingleApiVersion($"v{appInfo.Version}", $"{appInfo.Name}-Api");
                        c.ApplyDefaultApiConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });

                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
            {
                odataDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                });

                odataDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        EnvironmentAppInfo appInfo = DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().AppInfo;
                        c.SingleApiVersion($"v{appInfo.Version}", $"{appInfo.Name}-Api");
                        c.ApplyDefaultODataConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });

                odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();
            });

            if (DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().DebugMode == false)
                dependencyManager.RegisterSignalRConfiguration<SignalRSqlServerScaleoutConfiguration>();
            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration<BitChangeSetManagerAppMessageHubEvents>();

            dependencyManager.RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.RegisterRepository(typeof(BitChangeSetManagerEfRepository<>).GetTypeInfo());
            dependencyManager.RegisterRepository(typeof(ChangeSetRepository).GetTypeInfo());

            dependencyManager.RegisterEfDbContext<BitChangeSetManagerDbContext>();

            dependencyManager.RegisterDtoEntityMapper();

            dependencyManager.RegisterDtoEntityMapperConfiguration<DefaultDtoEntityMapperConfiguration>();
            dependencyManager.RegisterDtoEntityMapperConfiguration<BitChangeSetManagerDtoEntityMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<BitChangeSetManagerUserService, BitChangeSetManagerClientProvider>();

            dependencyManager.RegisterSecureIndexPageMiddlewareUsingDefaultConfiguration();

            dependencyManager.Register<IUserSettingProvider, BitUserSettingProvider>();
        }
    }
}
