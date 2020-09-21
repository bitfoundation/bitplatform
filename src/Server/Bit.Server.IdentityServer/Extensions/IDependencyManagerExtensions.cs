using Bit.IdentityServer;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations;
using Bit.Owin.Implementations;
using IdentityServer3.Core.Services;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterSingleSignOnServer<TUserService, TOAuthClientsProvider>(this IDependencyManager dependencyManager, string? name = null)
            where TUserService : class, IUserService
            where TOAuthClientsProvider : class, IOAuthClientsProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppCertificatesProvider, DefaultAppCertificatesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<IScopesProvider, DefaultScopesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IdentityServer3.Core.Logging.ILogProvider, DefaultIdentityServerLogProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>(name: name);
            dependencyManager.Register<IViewService, DefaultViewService>(overwriteExisting: false);
            dependencyManager.Register<IRedirectUriValidator, RegexBasedRedirectUriValidator>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IEventService, DefaultEventService>(overwriteExisting: false);

            dependencyManager.Register<IOAuthClientsProvider, TOAuthClientsProvider>(overwriteExisting: false);
            dependencyManager.Register<IUserService, TUserService>(overwriteExisting: false);

            dependencyManager.Register<IExternalIdentityProviderConfiguration, GoogleIdentityProviderConfiguration>(overwriteExisting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, FacebookIdentityProviderConfiguration>(overwriteExisting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, TwitterIdentityProviderConfiguration>(overwriteExisting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, MicrosoftIdentityProviderConfiguration>(overwriteExisting: false);
            dependencyManager.Register<IExternalIdentityProviderConfiguration, LinkedInIdentityProviderConfiguration>(overwriteExisting: false);

            return dependencyManager;
        }
    }
}
