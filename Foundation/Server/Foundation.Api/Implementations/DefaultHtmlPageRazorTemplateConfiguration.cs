using System;
using System.IO;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using RazorEngine;
using RazorEngine.Templating;

namespace Foundation.Api.Implementations
{
    public class DefaultHtmlPageRazorTemplateConfiguration : IAppEvents
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IPathProvider _pathProvider;

        protected DefaultHtmlPageRazorTemplateConfiguration()
        {
        }

        public DefaultHtmlPageRazorTemplateConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IPathProvider pathProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            _pathProvider = pathProvider;
        }

        public virtual void OnAppStartup()
        {
            string templateFilePath =
                _pathProvider.MapPath(_activeAppEnvironment.GetConfig<string>("DefaultPageTemplatePath"));

            string template = File.ReadAllText(templateFilePath);

            Engine.Razor.Compile(name: "defaultPageTemplate", modelType: typeof(IDependencyResolver),
                templateSource: new LoadedTemplateSource(template, templateFilePath));
        }

        public virtual void OnAppEnd()
        {

        }
    }
}