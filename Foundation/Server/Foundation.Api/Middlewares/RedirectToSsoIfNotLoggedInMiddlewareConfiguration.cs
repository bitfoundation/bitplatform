using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Microsoft.Owin;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class RedirectToSsoIfNotLoggedInMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly IPageRequestDetector _pageRequestDetector;

        public RedirectToSsoIfNotLoggedInMiddlewareConfiguration(IPageRequestDetector pageRequestDetector)
        {
            if (pageRequestDetector == null)
                throw new ArgumentNullException(nameof(pageRequestDetector));

            _pageRequestDetector = pageRequestDetector;
        }

        protected RedirectToSsoIfNotLoggedInMiddlewareConfiguration()
        {

        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.MapWhen(IsDefaultPageAndNotLoggedIn,
                innerApp => { innerApp.Use<RedirectToSsoIfNotLoggedInMiddleware>(); });
        }

        public virtual bool IsDefaultPageAndNotLoggedIn(IOwinContext cntx)
        {
            return _pageRequestDetector.IsAuthorizeRequiredPageRequest(cntx) &&
                   cntx.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated() == false;
        }
    }
}