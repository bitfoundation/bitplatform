using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Microsoft.Owin;
using NWebsec.Owin;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class DefaultPageMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IPageRequestDetector _pageRequestDetector;

        protected DefaultPageMiddlewareConfiguration()
        {
        }

        public DefaultPageMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider, IPageRequestDetector pageRequestDetector)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (pageRequestDetector == null)
                throw new ArgumentNullException(nameof(pageRequestDetector));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            _pageRequestDetector = pageRequestDetector;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.MapWhen(IsDefaultPageRequest, innerApp =>
            {
                if (_activeAppEnvironment.GetConfig("RequireSsl", defaultValueOnNotFound: false))
                {
                    innerApp.UseHsts(TimeSpan.FromDays(1));
                }

                innerApp.UseXContentTypeOptions();

                innerApp.UseXDownloadOptions();

                innerApp.UseXXssProtection(xssProtectionOptions => { xssProtectionOptions.EnabledWithBlockMode(); });

                innerApp.Use<OwinNoCacheResponseMiddleware>();

                innerApp.Use<DefaultPageMiddleware>();
            });
        }

        public virtual bool IsDefaultPageRequest(IOwinContext cntx)
        {
            return _pageRequestDetector.IsPageRequest(cntx);
        }
    }
}