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
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual IPathProvider PathProvider { get; set; }

        public virtual void OnAppStartup()
        {
            TemplateServiceConfiguration config = new TemplateServiceConfiguration();

            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            config.Debug = activeAppEnvironment.DebugMode;
            config.Language = Language.CSharp;
            config.EncodedStringFactory = new NullCompatibleEncodedStringFactory();

            IRazorEngineService service = RazorEngineService.Create(config);

            Engine.Razor = service;

            string indexPageFilePath = PathProvider.StaticFileMapPath(activeAppEnvironment.GetConfig("IndexPagePath", "indexPage.cshtml"));

            if (File.Exists(indexPageFilePath))
            {
                string indexPageContents = File.ReadAllText(indexPageFilePath);

                Engine.Razor.Compile(name: "indexPage", modelType: typeof(IDependencyResolver),
                    templateSource: new LoadedTemplateSource(indexPageContents, indexPageFilePath));
            }

            string loginPageFilePath = PathProvider.StaticFileMapPath(activeAppEnvironment.GetConfig("LoginPagePath", "loginPage.cshtml"));

            if (File.Exists(loginPageFilePath))
            {
                string loginPageContents = File.ReadAllText(loginPageFilePath);

                Engine.Razor.Compile(name: "loginPage", modelType: typeof(IDependencyResolver),
                    templateSource: new LoadedTemplateSource(loginPageContents, loginPageFilePath));
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
                    return _obj?.ToString() ?? string.Empty;
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