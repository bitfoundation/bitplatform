using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Hangfire.Implementations;
using Bit.Model.Implementations;
using Bit.OData.ActionFilters;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.Signalr.Implementations;
using Bit.Test;
using Bit.Tests.Data.Implementations;
using Bit.Tests.IdentityServer.Implementations;
using Bit.Tests.Model.Implementations;
using Bit.Tests.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Application;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Web.Http;

namespace Bit.Tests
{
    public class BitOwinTestAppModulesProvider : IAppModule, IAppModulesProvider
    {
        private readonly TestEnvironmentArgs _args;

        public BitOwinTestAppModulesProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        protected BitOwinTestAppModulesProvider()
        {

        }

        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(ConsoleLogStore).GetTypeInfo(), typeof(DebugLogStore).GetTypeInfo());

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();

            dependencyManager.RegisterAppEvents<InitialTestDataConfiguration>();

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterMinimalOwinMiddlewares();
            dependencyManager.RegisterSingleSignOnClient();

            dependencyManager.RegisterMetadata();

            dependencyManager.RegisterDefaultWebApiAndODataConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();

                webApiDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new AuthorizeAttribute());
                });

                webApiDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableMultiVersionWebApiSwaggerWithUI();
                });
            });

            dependencyManager.RegisterODataMiddleware(odataDependencyManager =>
            {
                odataDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration();

                odataDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                });

                odataDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("All", "Swagger-OData");
                        c.ApplyDefaultODataConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });
            });

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration();

            dependencyManager.RegisterHangfireBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.RegisterRepository(typeof(TestEfRepository<>).GetTypeInfo());

            dependencyManager.RegisterEfCoreDbContext<TestDbContext>((serviceProvider, optionsBuilder) =>
            {
                string connectionStringOrDatabaseName = serviceProvider.GetRequiredService<AppEnvironment>().GetConfig<string>("TestDbConnectionString");

                if (Settings.Default.UseInMemoryProviderByDefault)
                    optionsBuilder.UseInMemoryDatabase(connectionStringOrDatabaseName);
                else
                    optionsBuilder.UseSqlServer(serviceProvider.GetService<IDbConnectionProvider>().GetDbConnection(connectionStringOrDatabaseName, rollbackOnScopeStatusFailure: true));
            });

            dependencyManager.RegisterDtoEntityMapper();

            dependencyManager.RegisterMapperConfiguration<DefaultMapperConfiguration>();
            dependencyManager.RegisterMapperConfiguration<TestMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<TestUserService, TestOAuthClientsProvider>();

            _args?.AdditionalDependencies?.Invoke(dependencyManager, services);

            dependencyManager.RegisterSecureIndexPageMiddlewareUsingDefaultConfiguration();
        }

        public virtual IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }
    }
}
