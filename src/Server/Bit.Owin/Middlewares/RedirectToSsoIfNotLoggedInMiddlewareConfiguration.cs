using System;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Microsoft.Owin;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class RedirectToSsoIfNotLoggedInMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.MapWhen(IfIsNotLoggedIn,
                innerApp =>
                {
                    innerApp.Use<OwinNoCacheResponseMiddleware>();
                    innerApp.Use<RedirectToSsoIfNotLoggedInMiddleware>();
                });
        }

        public virtual bool IfIsNotLoggedIn(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.GetDependencyResolver().Resolve<IUserInformationProvider>().IsAuthenticated() == false;
        }
    }
}