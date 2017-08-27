using Bit.Core.Contracts;
using Bit.OwinCore.Contracts;
using IdentityServer3.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using System;
using Bit.Owin.Middlewares;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreSingleSignOnClientMiddlewareConfiguration : SingleSignOnClientMiddlewareConfiguration, IAspNetCoreMiddlewareConfiguration
    {
#if DEBUG
        protected AspNetCoreSingleSignOnClientMiddlewareConfiguration()
        {
        }
#endif

        public AspNetCoreSingleSignOnClientMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            ICertificateProvider certificateProvider) : base(appEnvironmentProvider, certificateProvider)
        {

        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            IdentityServerBearerTokenAuthenticationOptions authOptions = BuildIdentityServerBearerTokenAuthenticationOptions();

            aspNetCoreApp.UseIdentityServerBearerTokenAuthentication(authOptions);
        }
    }
}