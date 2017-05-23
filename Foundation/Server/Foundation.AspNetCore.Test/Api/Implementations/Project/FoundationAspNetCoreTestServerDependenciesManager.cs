using Bit.Core;
using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Metadata;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.JobScheduler.Implementations;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.AspNetCore.Contracts;
using Foundation.AspNetCore.Middlewares;
using Foundation.AspNetCore.Test.Api.Middlewares;
using Foundation.Core.Contracts;
using Foundation.Core.Implementations;
using Foundation.Test;
using Foundation.Test.Api.Middlewares;
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


        public FoundationAspNetCoreTestServerDependenciesManager(TestEnvironmentArgs args)
        {
            this._args = args;
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

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<ITestUserService, TestUserService>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterAspNetCoreMiddleware<TestWebApiCoreMvcMiddlewareConfiguration>(); //@Important

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ExtendAspNetCoreAutofacLifetimeToOwinMiddlewareConfiguration>(); //@Important
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            if (_args.UseSso)
                dependencyManager.RegisterOwinMiddleware<SingleSignOnMiddlewareConfiguration>();
            else
                dependencyManager.RegisterOwinMiddleware<EmbeddedOAuthMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.GetBitApiAssembly(), AssemblyContainer.Current.GetBitTestCoreAssembly());

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
            {
                httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
            });

            dependencyManager.RegisterUsing(() =>
           {
               return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
               {
                   childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                   childDependencyManager.RegisterEdmModelProvider<FoundationEdmModelProvider>();

               }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

           }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration(AssemblyContainer.Current.GetBitSignalRAssembly());

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationMetadataBuilder).GetTypeInfo().Assembly, typeof(FoundationAspNetCoreTestEdmModelProvider).GetTypeInfo().Assembly);

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);
        }

        public virtual void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager)
        {
            services.AddWebApiCore();
        }
    }
}
