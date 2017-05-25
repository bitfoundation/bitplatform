using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using Bit.Api.Implementations.Project;
using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Core.Implementations;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Hangfire.Middlewares.JobScheduler.Implementations;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.IdentityServer.Middlewares;
using Bit.Model.Implementations;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Bit.Signalr.Middlewares.Signalr.Implementations;
using Bit.Test;
using Bit.Tests.Api.Implementations.Project;
using Bit.Tests.Api.Middlewares;
using Bit.Tests.Data.Implementations;
using Bit.Tests.IdentityServer.Implementations;
using Bit.Tests.Model.Implementations;
using Bit.Tests.Properties;
using IdentityServer3.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Tests
{
    public class BitOwinCoreTestDependenciesManagerProvider : IAspNetCoreDependenciesManager, IDependenciesManagerProvider
    {
        private readonly TestEnvironmentArgs _args;

        protected BitOwinCoreTestDependenciesManagerProvider()
        {
        }


        public BitOwinCoreTestDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
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
            dependencyManager.Register<ILogStore, ConsoleLogStore>();
            dependencyManager.Register<IDbConnectionProvider, DefaultSqlDbConnectionProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();
            dependencyManager.RegisterAppEvents<InitialTestDataConfiguration>();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterOwinMiddleware<ExtendAspNetCoreAutofacLifetimeToOwinMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterSingleSignOnServer();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.GetBitApiAssembly(), AssemblyContainer.Current.GetBitTestsAssembly());

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new AuthorizeAttribute());
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
                    childDependencyManager.RegisterEdmModelProvider<TestEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration(AssemblyContainer.Current.GetBitSignalRAssembly());

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(AssemblyContainer.Current.GetBitMetadataAssembly(), AssemblyContainer.Current.GetBitTestsAssembly(), AssemblyContainer.Current.GetBitIdentityServerAssembly());

            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(TestEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterGeneric(typeof(IEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), typeof(TestEfEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            if (Settings.Default.UseInMemoryProviderByDefault)
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, InMemoryDbContextObjectsProvider>();
            else
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, SqlDbContextObjectsProvider>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<TestDtoModelMapperConfiguration>();

            #region IdentityServer

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>();
            dependencyManager.Register<IViewService, DefaultViewService>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISsoPageHtmlProvider, RazorSsoHtmlPageProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISSOPageModelProvider, DefaultSSOPageModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);

            #endregion

            #region IdentityServer.Test

            dependencyManager.Register<IClientProvider, TestClientProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IUserService, TestUserService>(lifeCycle: DependencyLifeCycle.SingleInstance);

            #endregion

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);

            dependencyManager.RegisterAspNetCoreMiddleware<TestWebApiCoreMvcMiddlewareConfiguration>(); //@Important
        }

        public virtual void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager)
        {
            services.AddWebApiCore();
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }
    }
}
