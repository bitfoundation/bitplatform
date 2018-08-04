using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Twitter;
using Owin;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class TwitterIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            if (AppEnvironment.HasConfig("TwitterClientId") && AppEnvironment.HasConfig("TwitterSecret"))
            {
                string twitterClientId = AppEnvironment.GetConfig<string>("TwitterClientId");
                string twitterSecret = AppEnvironment.GetConfig<string>("TwitterSecret");

                Task TwitterOnAuthenticated(TwitterAuthenticatedContext context)
                {
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token", context.AccessToken));
                    context.Identity.AddClaim(new System.Security.Claims.Claim("access_token_secret", context.AccessTokenSecret));

                    foreach (System.Security.Claims.Claim claim in context.Identity.Claims)
                    {
                        string claimType = claim.Type;
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
