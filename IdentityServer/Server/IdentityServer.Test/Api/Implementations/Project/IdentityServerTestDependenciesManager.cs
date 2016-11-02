using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Implementations;
using Foundation.Test.Api.Middlewares;
using IdentityServer.Api.Contracts;
using IdentityServer.Api.Implementations;
using IdentityServer.Api.Metadata.Views;
using IdentityServer.Api.Middlewares;
using IdentityServer3.Core.Services;
using System.Reflection;

namespace IdentityServer.Test.Api.Implementations.Project
{
    public class IdentityServerTestDependenciesManager : IDependenciesManager
    {
        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            #region Foundation

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.Register<ILogStore, ConsoleLogStore>();
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>();
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterOwinMiddleware<OwinCompressionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly, typeof(LoginViewMetadataBuilder).GetTypeInfo().Assembly);

            #endregion

            #region IdentityServer

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>();
            dependencyManager.Register<IViewService, DefaultViewService>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISsoPageHtmlProvider, RazorSsoHtmlPageProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ISSOPageModelProvider, DefaultSSOPageModelProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            #endregion

            #region IdentityServer.Test

            dependencyManager.Register<IClientProvider, TestClientProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            dependencyManager.Register<IUserService, TestUserService>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            #endregion
        }
    }
}
