using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreReadAuthTokenFromCookieMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            aspNetCoreApp.UseMiddleware<AspNetCoreReadAuthTokenFromCookieMiddleware>();
        }
    }
}