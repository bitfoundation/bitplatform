using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.OwinCore.Contracts;
using IdentityServer3.AccessTokenValidation;
using IdentityServer3.Core.Models;
using Microsoft.AspNetCore.Builder;
using Owin;
using System;
using System.Net.Http;

namespace Bit.Owin.Middlewares
{
    public class AspNetCoreSingleSignOnClientMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly ICertificateProvider _certificateProvider;

        protected AspNetCoreSingleSignOnClientMiddlewareConfiguration()
        {
        }

        public AspNetCoreSingleSignOnClientMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            ICertificateProvider certificateProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (certificateProvider == null)
                throw new ArgumentNullException(nameof(certificateProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _certificateProvider = certificateProvider;
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            IdentityServerBearerTokenAuthenticationOptions authOptions = new IdentityServerBearerTokenAuthenticationOptions
            {
                ClientId = activeAppEnvironment.Security.ClientName,
                Authority = activeAppEnvironment.GetSsoUrl(),
                DelayLoadMetadata = true,
                RequiredScopes = activeAppEnvironment.Security.Scopes,
                ClientSecret = activeAppEnvironment.Security.ClientSecret.Sha512(),
                EnableValidationResultCache = true,
                ValidationResultCacheDuration = TimeSpan.FromMinutes(15),
                // ValidationMode = ValidationMode.ValidationEndpoint,
                ValidationMode = ValidationMode.Local,
                PreserveAccessToken = true,
                SigningCertificate = _certificateProvider.GetSingleSignOnCertificate(),
                BackchannelHttpHandler = GetHttpClientHandler(nameof(IdentityServerBearerTokenAuthenticationOptions.BackchannelHttpHandler)),
                IntrospectionHttpHandler = GetHttpClientHandler(nameof(IdentityServerBearerTokenAuthenticationOptions.IntrospectionHttpHandler)),
                IssuerName = activeAppEnvironment.GetSsoUrl()
            };

            aspNetCoreApp.UseIdentityServerBearerTokenAuthentication(authOptions);
        }

        protected virtual HttpMessageHandler GetHttpClientHandler(string usage)
        {
            return new HttpClientHandler();
        }
    }
}