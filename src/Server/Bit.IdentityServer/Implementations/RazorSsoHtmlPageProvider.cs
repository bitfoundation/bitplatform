using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
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
            _pathProvider = pathProvider;
            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

#if DEBUG
        protected RazorSsoHtmlPageProvider()
        {
        }
#endif

        private string _result = null;
        private readonly IPathProvider _pathProvider;
        private readonly AppEnvironment _activeAppEnvironment;

        public virtual async Task<string> GetSsoPageAsync(CancellationToken cancellationToken)
        {
            return Engine.Razor.Run("ssoPageTemplate", typeof(IDependencyResolver), _dependencyResolver);
        }
    }
}
