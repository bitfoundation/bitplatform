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
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.HasConfig("GoogleClientId") && AppEnvironment.HasConfig("GoogleSecret"))
            {
                string googleClientId = AppEnvironment.GetConfig<string>("GoogleClientId");
                string googleSecret = AppEnvironment.GetConfig<string>("GoogleSecret");

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
