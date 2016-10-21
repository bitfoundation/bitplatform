using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Metadata;
using Foundation.Api.Metadata;
using Foundation.Api.Middlewares;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Implementations;
using Foundation.Test.Api.Implementations;
using IdentityServer.Api.Contracts;
using IdentityServer.Api.Implementations;
using IdentityServer.Api.Metadata.Views;
using IdentityServer.Api.Middlewares;
using IdentityServer3.Core.Services;

namespace IdentityServer.Test.Api.Implementations.Project
{
    public class IdentityServerTestDependenciesManager : IDependenciesManager
    {
        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            #region Foundation

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultDependenciesManagerProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);
            dependencyManager.RegisterInstance(DefaultDependencyManager.Current);

            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.Register<ILogStore, TestLogStore>();
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>();
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>();
            dependencyManager.Register<IMetadataBuilder, FoundationMetadataBuilder>(overwriteExciting: false);

            #endregion

            #region IdentityServer

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DepepdencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>();
            dependencyManager.Register<IMetadataBuilder, LoginViewMetadataBuilder>(overwriteExciting: false);
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
