using Bit.IdentityServer;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations;
using Bit.Owin.Implementations;
using IdentityServer3.Core.Services;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterSingleSignOnServer<TUserService, TClientProvider>(this IDependencyManager dependencyManager, string name = null)
            where TUserService : class, IUserService
            where TClientProvider : class, IClientProvider
        {
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>(name: name);
            dependencyManager.Register<IViewService, DefaultViewService>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ICustomLoginDataProvider, DefaultCustomLoginDataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ISsoPageHtmlProvider, RazorSsoHtmlPageProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ISSOPageModelProvider, DefaultSSOPageModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IRedirectUriValidator, RegexBasedRedirectUriValidator>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IEventService, DefaultEventService>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IClientProvider, TClientProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IUserService, TUserService>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IExternalIdentityProviderConfiguration, GoogleIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, FacebookIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, TwitterIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, MicrosoftIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, LinkedInIdentityProviderConfiguration>(overwriteExciting: false);

            return dependencyManager;
        }
    }
}
