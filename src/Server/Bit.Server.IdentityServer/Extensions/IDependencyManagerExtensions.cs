using Bit.IdentityServer;
using Bit.IdentityServer.Implementations;
using Bit.Owin.Implementations;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterSingleSignOnServer<TUserService>(this IDependencyManager dependencyManager, string? name = null)
            where TUserService : UserService
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppCertificatesProvider, DefaultAppCertificatesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterOwinMiddleware<IdentityServerMiddlewareConfiguration>(name: name);
            dependencyManager.Register<UserService, TUserService>(overwriteExisting: false);

            return dependencyManager;
        }
    }
}
