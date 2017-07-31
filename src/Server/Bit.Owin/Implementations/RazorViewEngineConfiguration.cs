using System;
using Bit.Core.Contracts;
using Bit.Core.Models;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using System.IO;

namespace Bit.Owin.Implementations
{
    public class RazorViewEngineConfiguration : IAppEvents
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly IPathProvider _pathProvider;

#if DEBUG
        protected RazorViewEngineConfiguration()
        {
        }
#endif

        public RazorViewEngineConfiguration(IAppEnvironmentProvider appEnvironmentProvider, IPathProvider pathProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _pathProvider = pathProvider;
        }

        public virtual void OnAppStartup()
        {
            TemplateServiceConfiguration config = new TemplateServiceConfiguration();

            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            config.Debug = activeAppEnvironment.DebugMode;
            config.Language = Language.CSharp;
            config.EncodedStringFactory = new NullCompatibleEncodedStringFactory();

            IRazorEngineService service = RazorEngineService.Create(config);

            Engine.Razor = service;

            string defaultPageTemplateFilePath = _pathProvider.StaticFileMapPath(activeAppEnvironment.GetConfig("DefaultPageTemplatePath", "defaultPageTemplate.cshtml"));

            if (File.Exists(defaultPageTemplateFilePath))
            {
                string defaultPageTemplateContents = File.ReadAllText(defaultPageTemplateFilePath);

                Engine.Razor.Compile(name: "defaultPageTemplate", modelType: typeof(IDependencyResolver),
                    templateSource: new LoadedTemplateSource(defaultPageTemplateContents, defaultPageTemplateFilePath));
            }

            string ssoPageTemplateFilePath = _pathProvider.StaticFileMapPath(activeAppEnvironment.GetConfig("SsoPageTemplatePath", "ssoPageTemplate.cshtml"));

            if (File.Exists(ssoPageTemplateFilePath))
            {
                string ssoPageTemplateContents = File.ReadAllText(ssoPageTemplateFilePath);

                Engine.Razor.Compile(name: "ssoPageTemplate", modelType: typeof(IDependencyResolver),
                    templateSource: new LoadedTemplateSource(ssoPageTemplateContents, ssoPageTemplateFilePath));
            }
        }

        public class NullCompatibleEncodedStringFactory : IEncodedStringFactory
        {
            public class DefaultEncodedString : IEncodedString
            {
                private readonly object _obj;

                public DefaultEncodedString(object obj)
                {
                    _obj = obj;
                }

                public virtual string ToEncodedString()
                {
                    return ToString();
                }

                public override string ToString()
                {
                    return _obj == null ? string.Empty : _obj.ToString();
                }
            }

            public virtual IEncodedString CreateEncodedString(object value)
            {
                return new DefaultEncodedString(value);
            }

            public virtual IEncodedString CreateEncodedString(string value)
            {
                return new DefaultEncodedString(value);
            }
        }

        public virtual void OnAppEnd()
        {
            Engine.Razor.Dispose();
        }
    }
}