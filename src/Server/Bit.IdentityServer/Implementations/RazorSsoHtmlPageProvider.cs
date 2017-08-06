using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.IdentityServer.Contracts;
using RazorEngine;
using RazorEngine.Templating;

namespace Bit.IdentityServer.Implementations
{
    public class RazorSsoHtmlPageProvider : ISsoPageHtmlProvider
    {
        private readonly IDependencyResolver _dependencyResolver;

        public RazorSsoHtmlPageProvider(IAppEnvironmentProvider appEnvironmentProvider, IPathProvider pathProvider, IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _dependencyResolver = dependencyManager;
            appEnvironmentProvider.GetActiveAppEnvironment();
        }

#if DEBUG
        protected RazorSsoHtmlPageProvider()
        {
        }
#endif

        public virtual async Task<string> GetSsoPageAsync(CancellationToken cancellationToken)
        {
            return Engine.Razor.Run("ssoPageTemplate", typeof(IDependencyResolver), _dependencyResolver);
        }
    }
}
