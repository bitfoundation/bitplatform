using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Services.Identity;

public class AppBearerTokenOptionsSetup(IConfiguration configuration,
    IServiceProvider serviceProvider) : IPostConfigureOptions<BearerTokenOptions>
{
    public void PostConfigure(string? name, BearerTokenOptions options)
    {
        options.BearerTokenProtector = ActivatorUtilities.CreateInstance<AppJwtSecureDataFormat>(serviceProvider, "AccessToken");
        options.RefreshTokenProtector = ActivatorUtilities.CreateInstance<AppJwtSecureDataFormat>(serviceProvider, "RefreshToken");

        options.Events = new()
        {
            OnMessageReceived = async context =>
            {
                // The server accepts the accessToken from either the authorization header, the cookie, or the request URL query string
                context.Token ??= context.Request.Query.ContainsKey("access_token") ? context.Request.Query["access_token"] : context.Request.Cookies["access_token"];
            }
        };

        configuration.GetRequiredSection("Identity").Bind(options);
    }
}
