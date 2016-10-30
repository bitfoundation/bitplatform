using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.AspNetCore.Contracts;
using Foundation.AspNetCore.Middlewares;
using Foundation.AspNetCore.Test.Api.Middlewares;
using Foundation.Core.Contracts;
using Foundation.Core.Implementations;
using Foundation.Test;
using Foundation.Test.Api.Implementations;
using Foundation.Test.Api.Middlewares;
using Foundation.Test.Api.Middlewares.JobScheduler.Implementations;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Foundation.AspNetCore.Test.Api.Implementations.Project
{
    public class FoundationAspNetCoreTestServerDependenciesManager : IAspNetCoreDependenciesManager
    {
        private readonly TestEnvironmentArgs _args;

        protected FoundationAspNetCoreTestServerDependenciesManager()
        {
        }


        public FoundationAspNetCoreTestServerDependenciesManager(TestEnvironmentArgs _args)
        {
            this._args = _args;
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
            dependencyManager.Register<ILogStore, TestLogStore>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IPageRequestDetector, DefaultPageRequestDetector>(lifeCycle: DepepdencyLifeCycle.SingleInstance); //@Important
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<INameService, DefaultNameService>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ITestUserService, TestUserService>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<FoundationInitialNameServiceConfiguration>();

            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterAspNetCoreMiddleware<TestWebApiCoreMvcMiddlewareConfiguration>(); //@Important

            dependencyManager.RegisterOwinMiddleware<OwinCompressionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ExtendAspNetCoreAutofacLifetimeToOwinMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            if (_args.UseSso)
                dependencyManager.RegisterOwinMiddleware<SingleSignOnMiddlewareConfiguration>();
            else
                dependencyManager.RegisterOwinMiddleware<EmbeddedOAuthMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(FoundationAspNetCoreTestEdmModelProvider).GetTypeInfo().Assembly);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
            {
                httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
            });

            dependencyManager.RegisterUsing(resolver =>
            {
                return dependencyManager.CreateChildDependencyManager(childDependencyManager =>
                {
                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<FoundationEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DepepdencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthroizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration(typeof(MessagesHub).GetTypeInfo().Assembly);

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(FoundationAspNetCoreTestEdmModelProvider).GetTypeInfo().Assembly);

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);
        }

        public virtual void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager)
        {
            services.AddWebApiCore();
        }
    }
}
