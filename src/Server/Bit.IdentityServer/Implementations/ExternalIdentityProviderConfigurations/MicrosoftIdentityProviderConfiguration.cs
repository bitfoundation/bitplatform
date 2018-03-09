using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.MicrosoftAccount;
using Newtonsoft.Json.Linq;
using Owin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class MicrosoftIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            if (activeAppEnvironment.HasConfig("MicrosoftClientId") && activeAppEnvironment.HasConfig("MicrosoftSecret"))
            {
                string microsoftClientId = activeAppEnvironment.GetConfig<string>("MicrosoftClientId");
                string microsoftSecret = activeAppEnvironment.GetConfig<string>("MicrosoftSecret");

                MicrosoftAccountAuthenticationOptions microsoftAccountAuthenticationOptions = new MicrosoftAccountAuthenticationOptions
                {
                    AuthenticationType = "Microsoft",
                    SignInAsAuthenticationType = signInType,
                    ClientId = microsoftClientId,
                    ClientSecret = microsoftSecret,
                    Provider = new MicrosoftAccountAuthenticationProvider
                    {
                        OnAuthenticated = context =>
                        {
                            context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));

                            foreach (KeyValuePair<string, JToken> claim in context.User)
                            {
                                string claimType = $"{claim.Key}";
                                string claimValue = claim.Value.ToString();

                                if (!context.Identity.HasClaim(claimType, claimValue))
                                    context.Identity.AddClaim(new System.Security.Claims.Claim(claimType, claimValue, "XmlSchemaString", "Microsoft"));
                            }

                            return Task.CompletedTask;
                        }
                    }
                };

                owinApp.UseMicrosoftAccountAuthentication(microsoftAccountAuthenticationOptions);
            }
        }
    }
}
