using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Microsoft.Owin.Security.Twitter;
using Owin;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations.ExternalIdentityProviderConfigurations
{
    public class TwitterIdentityProviderConfiguration : IExternalIdentityProviderConfiguration
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual void ConfiguerExternalIdentityProvider(IAppBuilder owinApp, string signInType)
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            if (activeAppEnvironment.HasConfig("TwitterClientId") && activeAppEnvironment.HasConfig("TwitterSecret"))
            {
                string twitterClientId = activeAppEnvironment.GetConfig<string>("TwitterClientId");
                string twitterSecret = activeAppEnvironment.GetConfig<string>("TwitterSecret");

                TwitterAuthenticationOptions twitterAuthenticationOptions = new TwitterAuthenticationOptions
                {
                    AuthenticationType = "Twitter",
                    SignInAsAuthenticationType = signInType,
                    ConsumerKey = twitterClientId,
                    ConsumerSecret = twitterSecret,
                    Provider = new TwitterAuthenticationProvider
                    {
                        OnAuthenticated = context =>
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
                    }
                };

                owinApp.UseTwitterAuthentication(twitterAuthenticationOptions);
            }
        }
    }
}
