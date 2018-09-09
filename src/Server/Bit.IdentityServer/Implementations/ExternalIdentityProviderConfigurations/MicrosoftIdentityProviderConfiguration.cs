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
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void ConfigureExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.HasConfig("MicrosoftClientId") && AppEnvironment.HasConfig("MicrosoftSecret"))
            {
                string microsoftClientId = AppEnvironment.GetConfig<string>("MicrosoftClientId");
                string microsoftSecret = AppEnvironment.GetConfig<string>("MicrosoftSecret");

                Task MicrosoftOnAuthenticated(MicrosoftAccountAuthenticatedContext context)
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

                MicrosoftAccountAuthenticationOptions microsoftAccountAuthenticationOptions = new MicrosoftAccountAuthenticationOptions
                {
                    AuthenticationType = "Microsoft",
                    SignInAsAuthenticationType = signInType,
                    ClientId = microsoftClientId,
                    ClientSecret = microsoftSecret,
                    Provider = new MicrosoftAccountAuthenticationProvider
                    {
                        OnAuthenticated = MicrosoftOnAuthenticated
                    }
                };

                owinApp.UseMicrosoftAccountAuthentication(microsoftAccountAuthenticationOptions);
            }
        }
    }
}
