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
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void ConfigureExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.FacebookClientId, out string? facebookClientId) && AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.FacebookSecret, out string? facebookSecret) && facebookClientId != null && facebookSecret != null)
            {
                Task FacebookOnAuthenticated(FacebookAuthenticatedContext context)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));

                    foreach (KeyValuePair<string, JToken?> claim in context.User)
                    {
                        string claimType = $"{claim.Key}";
                        string? claimValue = claim.Value?.ToString();

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
