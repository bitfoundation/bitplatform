using Bit.IdentityServer;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using IdentityServer3.Core.Services;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterSingleSignOnServer<TUserService, TOAuthClientsProvider>(this IDependencyManager dependencyManager, string name = null)
            where TUserService : class, IUserService
            where TOAuthClientsProvider : class, IOAuthClientsProvider
        {
            dependencyManager.Register<IAppCertificatesProvider, DefaultAppCertificatesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>(name: name);
            dependencyManager.Register<IViewService, DefaultViewService>(overwriteExciting: false);
            dependencyManager.Register<IRedirectUriValidator, RegexBasedRedirectUriValidator>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IEventService, DefaultEventService>(overwriteExciting: false);

            dependencyManager.Register<IOAuthClientsProvider, TOAuthClientsProvider>(overwriteExciting: false);
            dependencyManager.Register<IUserService, TUserService>(overwriteExciting: false);

            dependencyManager.Register<IExternalIdentityProviderConfiguration, GoogleIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, FacebookIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, TwitterIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, MicrosoftIdentityProviderConfiguration>(overwriteExciting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, LinkedInIdentityProviderConfiguration>(overwriteExciting: false);

            return dependencyManager;
        }
    }
}
