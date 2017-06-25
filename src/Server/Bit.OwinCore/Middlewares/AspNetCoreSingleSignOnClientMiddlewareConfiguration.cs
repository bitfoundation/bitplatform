using Bit.Core.Contracts;
using Bit.OwinCore.Contracts;
using IdentityServer3.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class AspNetCoreSingleSignOnClientMiddlewareConfiguration : SingleSignOnClientMiddlewareConfiguration, IAspNetCoreMiddlewareConfiguration
    {
        protected AspNetCoreSingleSignOnClientMiddlewareConfiguration()
        {
        }

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