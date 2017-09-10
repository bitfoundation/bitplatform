using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thinktecture.IdentityModel.Owin;

namespace Owin
{
    public static class IAppBuilderExtensions
    {
        public static void UseBasicAuthentication(this IAppBuilder owinApp, Func<string, string, Task<bool>> userPassValidator)
        {
            owinApp.UseBasicAuthentication(new BasicAuthenticationOptions("BasicAuth", async (username, password) =>
            {
                if (await userPassValidator(username, password))
                {
                    return new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("primary_sid", username),
                        new System.Security.Claims.Claim("client_id", "basic-auth")
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
