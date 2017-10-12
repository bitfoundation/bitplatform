using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using IdentityServer3.AccessTokenValidation;
using Owin;
using System;
using System.Net.Http;

namespace Bit.Owin.Middlewares
{
    public class SingleSignOnClientMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual ICertificateProvider CertificateProvider { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            IdentityServerBearerTokenAuthenticationOptions authOptions = BuildIdentityServerBearerTokenAuthenticationOptions();

            owinApp.UseIdentityServerBearerTokenAuthentication(authOptions);
        }

        public virtual IdentityServerBearerTokenAuthenticationOptions BuildIdentityServerBearerTokenAuthenticationOptions()
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            IdentityServerBearerTokenAuthenticationOptions authOptions = new IdentityServerBearerTokenAuthenticationOptions
            {
                ClientId = activeAppEnvironment.Security.ClientId,
                Authority = activeAppEnvironment.GetSsoUrl(),
                DelayLoadMetadata = true,
                RequiredScopes = activeAppEnvironment.Security.Scopes,
                ClientSecret = activeAppEnvironment.Security.ClientSecret,
                EnableValidationResultCache = true,
                ValidationResultCacheDuration = TimeSpan.FromMinutes(15),
                // ValidationMode = ValidationMode.ValidationEndpoint,
                ValidationMode = ValidationMode.Local,
                PreserveAccessToken = true,
                SigningCertificate = CertificateProvider.GetSingleSignOnCertificate(),
                BackchannelHttpHandler = GetHttpClientHandler(nameof(IdentityServerBearerTokenAuthenticationOptions.BackchannelHttpHandler)),
                IntrospectionHttpHandler = GetHttpClientHandler(nameof(IdentityServerBearerTokenAuthenticationOptions.IntrospectionHttpHandler)),
                IssuerName = activeAppEnvironment.GetSsoIssuerName()
            };

            return authOptions;
        }

        protected virtual HttpMessageHandler GetHttpClientHandler(string usage)
        {
            return new HttpClientHandler();
        }
    }
}