using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Google;
using Newtonsoft.Json.Linq;
using Owin;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                    ClientSecret = googleSecret,
                    Provider = new GoogleOAuth2AuthenticationProvider
                    {
                        OnAuthenticated = context =>
                        {
                            context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));

                            foreach (KeyValuePair<string, JToken> claim in context.User)
                            {
                                string claimType = $"{claim.Key}";
                                string claimValue = claim.Value.ToString();

                                if (!context.Identity.HasClaim(claimType, claimValue))
                                    context.Identity.AddClaim(new System.Security.Claims.Claim(claimType, claimValue, "XmlSchemaString", "Google"));
                            }

                            return Task.CompletedTask;
                        }
                    }
                });
            }
        }
    }
}
