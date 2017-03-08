using Foundation.Core.Contracts;
using IdentityServer.Api.Contracts;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation.Core.Models;

namespace IdentityServer.Api.Implementations
{
    public class RazorSsoHtmlPageProvider : ISsoPageHtmlProvider
    {
        private readonly IDependencyManager _dependencyManager;

        public RazorSsoHtmlPageProvider(IAppEnvironmentProvider appEnvironmentProvider, IPathProvider pathProvider, IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _dependencyManager = dependencyManager;
            _pathProvider = pathProvider;
            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        protected RazorSsoHtmlPageProvider()
        {

        }

        private string _result = null;
        private readonly IPathProvider _pathProvider;
        private readonly AppEnvironment _activeAppEnvironment;

        public virtual async Task<string> GetSsoPageAsync(CancellationToken cancellationToken)
        {
            if (_result == null)
            {
                string templateFilePath = _pathProvider.StaticFileMapPath(_activeAppEnvironment.GetConfig<string>("SsoPageTemplatePath"));

                string template = File.ReadAllText(templateFilePath);

                Engine.Razor.Compile(name: "ssoPageTemplate", modelType: typeof(IDependencyManager),
                    templateSource: new LoadedTemplateSource(template, templateFilePath));

                _result = Engine.Razor.Run("ssoPageTemplate", typeof(IDependencyManager), _dependencyManager);
            }

            return _result;
        }
    }
}
