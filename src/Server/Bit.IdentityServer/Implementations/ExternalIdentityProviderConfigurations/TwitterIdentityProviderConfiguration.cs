using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Twitter;
using Owin;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class TwitterIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void ConfigureExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.TwitterClientId, out string? twitterClientId) && AppEnvironment.TryGetConfig(AppEnvironment.KeyValues.IdentityServer.TwitterSecret, out string? twitterSecret) && twitterClientId != null && twitterSecret != null)
            {
                Task TwitterOnAuthenticated(TwitterAuthenticatedContext context)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token_secret", context.AccessTokenSecret));

                    foreach (System.Security.Claims.Claim claim in context.Identity.Claims)
                    {
                        string claimType = $"{claim.Type}";
                        string claimValue = claim.Value;

                        if (!context.Identity.HasClaim(claimType, claimValue))
                            context.Identity.AddClaim(new System.Security.Claims.Claim(claimType, claimValue, "XmlSchemaString", "Twitter"));
                    }

                    return Task.CompletedTask;
                }

                TwitterAuthenticationOptions twitterAuthenticationOptions = new TwitterAuthenticationOptions
                {
                    AuthenticationType = "Twitter",
                    SignInAsAuthenticationType = signInType,
                    ConsumerKey = twitterClientId,
                    ConsumerSecret = twitterSecret,
                    Provider = new TwitterAuthenticationProvider
                    {
                        OnAuthenticated = TwitterOnAuthenticated
                    }
                };

                owinApp.UseTwitterAuthentication(twitterAuthenticationOptions);
            }
        }
    }
}
