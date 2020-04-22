using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreSingleSignOnClientMiddlewareConfiguration : SingleSignOnClientMiddlewareConfiguration, IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition => MiddlewarePosition.BeforeOwinMiddlewares;

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            aspNetCoreApp.UseOwinApp(UseJwtBearerAuthentication);
        }
    }
}