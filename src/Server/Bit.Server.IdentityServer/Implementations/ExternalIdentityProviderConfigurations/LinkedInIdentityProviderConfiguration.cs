using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Newtonsoft.Json.Linq;
using Owin;
using Owin.Security.Providers.LinkedIn;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class LinkedInIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void ConfigureExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.LinkedInClientId, out string? linkedInClientId) && AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.LinkedInSecret, out string? linkedInSecret) && linkedInClientId != null && linkedInSecret != null)
            {
                Task LinkedInOnAuthenticated(LinkedInAuthenticatedContext context)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));

                    foreach (KeyValuePair<string, JToken?> claim in context.User)
                    {
                        string claimType = $"{claim.Key}";
                        string? claimValue = claim.Value?.ToString();

                        if (!context.Identity.HasClaim(claimType, claimValue))
                            context.Identity.AddClaim(new System.Security.Claims.Claim(claimType, claimValue, "XmlSchemaString", "LinkedIn"));
                    }

                    return Task.CompletedTask;
                }

                LinkedInAuthenticationOptions linkedInAuthenticationOptions = new LinkedInAuthenticationOptions
                {
                    AuthenticationType = "LinkedIn",
                    SignInAsAuthenticationType = signInType,
                    ClientId = linkedInClientId,
                    ClientSecret = linkedInSecret,
                    Provider = new LinkedInAuthenticationProvider
                    {
                        OnAuthenticated = LinkedInOnAuthenticated
                    }
                };

                owinApp.UseLinkedInAuthentication(linkedInAuthenticationOptions);
            }
        }
    }
}
