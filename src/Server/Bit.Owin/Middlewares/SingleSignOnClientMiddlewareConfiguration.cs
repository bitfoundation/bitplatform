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
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;
        private readonly ICertificateProvider _certificateProvider;

#if DEBUG
        protected SingleSignOnClientMiddlewareConfiguration()
        {
        }
#endif

        public SingleSignOnClientMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            ICertificateProvider certificateProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (certificateProvider == null)
                throw new ArgumentNullException(nameof(certificateProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
            _certificateProvider = certificateProvider;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            IdentityServerBearerTokenAuthenticationOptions authOptions = BuildIdentityServerBearerTokenAuthenticationOptions();

            owinApp.UseIdentityServerBearerTokenAuthentication(authOptions);
        }

        public virtual IdentityServerBearerTokenAuthenticationOptions BuildIdentityServerBearerTokenAuthenticationOptions()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

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
                SigningCertificate = _certificateProvider.GetSingleSignOnCertificate(),
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