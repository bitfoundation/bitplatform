using System;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace Foundation.Api.Implementations
{
    public class RazorViewEngineConfiguration : IAppEvents
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        protected RazorViewEngineConfiguration()
        {
        }

        public RazorViewEngineConfiguration(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
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