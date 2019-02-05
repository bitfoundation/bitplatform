using Bit.Core.Contracts;
using Bit.Core.Extensions;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.IdentityServer
{

    public class IdentityServerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }
        public virtual IAppCertificatesProvider AppCertificatesProvider { get; set; }
        public virtual IDependencyManager DependencyManager { get; set; }
        public virtual IScopesProvider ScopesProvider { get; set; }
        public virtual IRedirectUriValidator RedirectUriValidator { get; set; }
        public virtual IEventService EventService { get; set; }
        public virtual IEnumerable<IExternalIdentityProviderConfiguration> ExternalIdentityProviderConfigurations { get; set; }
        public virtual IEnumerable<IIdentityServerOptionsCustomizer> Customizers { get; set; }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/core", coreApp =>
            {
                LogProvider.SetCurrentLogProvider(DependencyManager.Resolve<ILogProvider>());

                IdentityServerServiceFactory factory = new IdentityServerServiceFactory()
                    .UseInMemoryClients(DependencyManager.Resolve<IOAuthClientsProvider>().GetClients().ToArray())
                    .UseInMemoryScopes(ScopesProvider.GetScopes());

                IUserService ResolveUserService(IdentityServer3.Core.Services.IDependencyResolver resolver)
                {
                    OwinEnvironmentService owinEnv = resolver.Resolve<OwinEnvironmentService>();
                    IOwinContext owinContext = new OwinContext(owinEnv.Environment);
                    IUserService userService = owinContext.GetDependencyResolver().Resolve<IUserService>();
                    return userService;
                }

                factory.UserService = new Registration<IUserService>(ResolveUserService);

                factory.EventService = new Registration<IEventService>(EventService);

                IViewService ResolveViewService(IdentityServer3.Core.Services.IDependencyResolver resolver)
                {
                    OwinEnvironmentService owinEnv = resolver.Resolve<OwinEnvironmentService>();
                    IOwinContext owinContext = new OwinContext(owinEnv.Environment);
                    return owinContext.GetDependencyResolver().Resolve<IViewService>();
                }

                factory.ViewService = new Registration<IViewService>(ResolveViewService);

                factory.RedirectUriValidator = new Registration<IRedirectUriValidator>(RedirectUriValidator);

                bool requireSslConfigValue = AppEnvironment.GetConfig("RequireSsl", defaultValueOnNotFound: false);

                string identityServerSiteName = AppEnvironment.GetConfig("IdentityServerSiteName", $"{AppEnvironment.AppInfo.Name} Identity Server");

                IdentityServerOptions identityServerOptions = new IdentityServerOptions
                {
                    SiteName = identityServerSiteName,
                    SigningCertificate = AppCertificatesProvider.GetSingleSignOnCertificate(),
                    Factory = factory,
                    RequireSsl = requireSslConfigValue,
                    EnableWelcomePage = AppEnvironment.DebugMode == true,
                    IssuerUri = AppEnvironment.GetSsoIssuerName(),
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

                foreach (IIdentityServerOptionsCustomizer customizer in Customizers)
                {
                    customizer.Customize(identityServerOptions);
                }

                coreApp.UseIdentityServer(identityServerOptions);
            });
        }

        protected virtual void ConfigureIdentityProviders(IAppBuilder owinApp, string signInAsType)
        {
            if (PlatformUtilities.IsRunningOnDotNetCore)
                return;

            foreach (IExternalIdentityProviderConfiguration externalIdentityProviderConfiguration in ExternalIdentityProviderConfigurations)
            {
                externalIdentityProviderConfiguration.ConfigureExternalIdentityProvider(owinApp, signInAsType);
            }
        }
    }
}
