using System;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Microsoft.Owin;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class RedirectToSsoIfNotLoggedInMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.MapWhen(IsDefaultPageAndNotLoggedIn,
                innerApp =>
                {
                    innerApp.Use<OwinNoCacheResponseMiddleware>();
                    innerApp.Use<RedirectToSsoIfNotLoggedInMiddleware>();
                });
        }

        public virtual bool IsDefaultPageAndNotLoggedIn(IOwinContext cntx)
        {
            return cntx.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated() == false;
        }
    }
}