using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Facebook;
using Newtonsoft.Json.Linq;
using Owin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class FacebookIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.HasConfig("FacebookClientId") && AppEnvironment.HasConfig("FacebookSecret"))
            {
                string facebookClientId = AppEnvironment.GetConfig<string>("FacebookClientId");
                string facebookSecret = AppEnvironment.GetConfig<string>("FacebookSecret");

                Task FacebookOnAuthenticated(FacebookAuthenticatedContext context)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));

                    foreach (KeyValuePair<string, JToken> claim in context.User)
                    {
                        string claimType = $"{claim.Key}";
                        string claimValue = claim.Value.ToString();

                        if (!context.Identity.HasClaim(claimType, claimValue))
                            context.Identity.AddClaim(new System.Security.Claims.Claim(claimType, claimValue, "XmlSchemaString", "Facebook"));
                    }

                    return Task.CompletedTask;
                }

                FacebookAuthenticationOptions facebookAuthenticationOptions = new FacebookAuthenticationOptions
                {
                    AuthenticationType = "Facebook",
                    SignInAsAuthenticationType = signInType,
                    AppId = facebookClientId,
                    AppSecret = facebookSecret,
                    Provider = new FacebookAuthenticationProvider
                    {
                        OnAuthenticated = FacebookOnAuthenticated
                    }
                };

                facebookAuthenticationOptions.Fields.Add("email");
                facebookAuthenticationOptions.Fields.Add("first_name");
                facebookAuthenticationOptions.Fields.Add("last_name");

                owinApp.UseFacebookAuthentication(facebookAuthenticationOptions);
            }
        }
    }
}
