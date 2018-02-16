using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Google;
using Owin;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class GoogleIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            if (activeAppEnvironment.HasConfig("GoogleClientId") && activeAppEnvironment.HasConfig("GoogleSecret"))
            {
                string googleClientId = activeAppEnvironment.GetConfig<string>("GoogleClientId");
                string googleSecret = activeAppEnvironment.GetConfig<string>("GoogleSecret");

                owinApp.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
                {
                    AuthenticationType = "Google",
                    SignInAsAuthenticationType = signInType,
                    ClientId = googleClientId,
                    ClientSecret = googleSecret
                });
            }
        }
    }
}
