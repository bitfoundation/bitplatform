using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using Microsoft.Owin.Security.Google;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.IdentityServer
{

    public class IdentityServerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }
        public virtual ICertificateProvider CertificateProvider { get; set; }
        public virtual IDependencyManager DependencyManager { get; set; }
        public virtual IScopesProvider ScopesProvider { get; set; }
        public virtual IRedirectUriValidator RedirectUriValidator { get; set; }
        public virtual IEventService EventService { get; set; }
        public virtual IEnumerable<IExternalIdentityProviderConfiguration> ExternalIdentityProviderConfigurations { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/core", coreApp =>
            {
                LogProvider.SetCurrentLogProvider(DependencyManager.Resolve<ILogProvider>());

                AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

                IdentityServerServiceFactory factory = new IdentityServerServiceFactory()
                    .UseInMemoryClients(DependencyManager.Resolve<IClientProvider>().GetClients().ToArray())
                    .UseInMemoryScopes(ScopesProvider.GetScopes());

                factory.UserService =
                    new Registration<IUserService>(DependencyManager.Resolve<IUserService>());

                factory.EventService = new Registration<IEventService>(EventService);

                factory.ViewService = new Registration<IViewService>(DependencyManager.Resolve<IViewService>());

                factory.RedirectUriValidator = new Registration<IRedirectUriValidator>(RedirectUriValidator);

                bool requireSslConfigValue = activeAppEnvironment.GetConfig("RequireSsl", defaultValueOnNotFound: false);

                string identityServerSiteName = activeAppEnvironment.GetConfig("IdentityServerSiteName", $"{activeAppEnvironment.AppInfo.Name} Identity Server");

                IdentityServerOptions identityServerOptions = new IdentityServerOptions
                {
                    SiteName = identityServerSiteName,
                    SigningCertificate = CertificateProvider.GetSingleSignOnCertificate(),
                    Factory = factory,
                    RequireSsl = requireSslConfigValue,
                    EnableWelcomePage = activeAppEnvironment.DebugMode == true,
                    IssuerUri = activeAppEnvironment.GetSsoIssuerName(),
                    CspOptions = new CspOptions
                    {
                        // Content security policy
                        Enabled = false
                    },
                    Endpoints = new EndpointOptions
                    {
                        EnableAccessTokenValidationEndpoint = true,
                        EnableAuthorizeEndpoint = true,
                        EnableCheckSessionEndpoint = true,
                        EnableClientPermissionsEndpoint = true,
                        EnableCspReportEndpoint = true,
                        EnableDiscoveryEndpoint = true,
                        EnableEndSessionEndpoint = true,
                        EnableIdentityTokenValidationEndpoint = true,
                        EnableIntrospectionEndpoint = true,
                        EnableTokenEndpoint = true,
                        EnableTokenRevocationEndpoint = true,
                        EnableUserInfoEndpoint = true
                    },
                    EventsOptions = new EventsOptions
                    {
                        RaiseErrorEvents = true,
                        RaiseFailureEvents = true
                    },
                    AuthenticationOptions = new AuthenticationOptions
                    {
                        IdentityProviders = ConfigureIdentityProviders
                    }
                };

                coreApp.UseIdentityServer(identityServerOptions);
            });
        }

        protected virtual void ConfigureIdentityProviders(IAppBuilder owinApp, string signInAsType)
        {
            foreach (IExternalIdentityProviderConfiguration externalIdentityProviderConfiguration in ExternalIdentityProviderConfigurations)
            {
                externalIdentityProviderConfiguration.ConfiguerExternalIdentityProvider(owinApp, signInAsType);
            }
        }
    }
}