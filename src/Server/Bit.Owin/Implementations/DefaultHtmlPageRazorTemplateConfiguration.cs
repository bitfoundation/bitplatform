using System;
using System.IO;
using Bit.Core.Contracts;
using Bit.Core.Models;
using RazorEngine;
using RazorEngine.Templating;

namespace Bit.Owin.Implementations
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
            string templateFilePath = _pathProvider.StaticFileMapPath(_activeAppEnvironment.GetConfig("DefaultPageTemplatePath", "defaultPageTemplate.cshtml"));

            string template = File.ReadAllText(templateFilePath);

            Engine.Razor.Compile(name: "defaultPageTemplate", modelType: typeof(IDependencyResolver),
                templateSource: new LoadedTemplateSource(template, templateFilePath));
        }

        public virtual void OnAppEnd()
        {

        }
    }
}