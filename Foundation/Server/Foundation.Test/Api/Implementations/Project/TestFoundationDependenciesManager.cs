using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.JobScheduler.Implementations;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Implementations;
using Foundation.DataAccess.Contracts;
using Foundation.DataAccess.Implementations;
using Foundation.DataAccess.Implementations.EntityFrameworkCore;
using Foundation.Model.Implementations;
using Foundation.Test.Api.Middlewares;
using Foundation.Test.DataAccess.Implementations;
using Foundation.Test.Model.Implementations;
using Foundation.Test.Properties;
using System.Reflection;
using System.Web.Http;

namespace Foundation.Test.Api.Implementations.Project
{
    public class TestFoundationDependenciesManager : IDependenciesManager
    {
        private readonly TestEnvironmentArgs _args;

        public TestFoundationDependenciesManager(TestEnvironmentArgs args)
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
            dependencyManager.Register<ITestUserService, TestUserService>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();
            dependencyManager.RegisterAppEvents<InitialTestDataConfiguration>();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignOutPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLogOutMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLoginMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            if (_args?.UseSso == true)
                dependencyManager.RegisterOwinMiddleware<SingleSignOnMiddlewareConfiguration>();
            else
                dependencyManager.RegisterOwinMiddleware<EmbeddedOAuthMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(TestEdmModelProvider).GetTypeInfo().Assembly);

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
                    childDependencyManager.RegisterEdmModelProvider<FoundationEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<TestEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration(typeof(MessagesHub).GetTypeInfo().Assembly);

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(TestEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(TestEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterGeneric(typeof(IEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), typeof(TestEfEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            if (Settings.Default.UseInMemoryProviderByDefault)
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, InMemoryDbContextObjectsProvider>();
            else
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, SqlDbContextObjectsProvider>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<TestDtoModelMapperConfiguration>();

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();
        }
    }
}
