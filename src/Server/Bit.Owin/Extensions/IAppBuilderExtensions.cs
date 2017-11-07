using System.Collections.Generic;
using System.Threading.Tasks;
using Thinktecture.IdentityModel.Owin;

namespace Owin
{
    public delegate Task<string> BasicAuthUserPassValidator(string userName, string password);

    public static class IAppBuilderExtensions
    {
        public static void UseBasicAuthentication(this IAppBuilder owinApp, BasicAuthUserPassValidator userPassValidator)
        {
            owinApp.UseBasicAuthentication(new BasicAuthenticationOptions("BasicAuth", async (username, password) =>
            {
                string userId = await userPassValidator(username, password);

                if (!string.IsNullOrEmpty(userId))
                {
                    return new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("client_id", "basic-auth"),
                        new System.Security.Claims.Claim("sub", userId),
                        new System.Security.Claims.Claim("primary_sid", userId),
                        new System.Security.Claims.Claim("upn", userId),
                        new System.Security.Claims.Claim("name", userId),
                        new System.Security.Claims.Claim("given_name", userId)
                    };
                }
                else
                {
                    return null;
                }
            }));
        }
    }
}
