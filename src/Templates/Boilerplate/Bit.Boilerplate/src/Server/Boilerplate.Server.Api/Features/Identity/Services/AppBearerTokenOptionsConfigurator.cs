using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Features.Identity.Services;

public class AppBearerTokenOptionsConfigurator(IConfiguration configuration,
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
                // The server accepts the accessToken from either the authorization header or the cookie.
                context.Token ??= context.HttpContext.GetAccessToken();
            }
        };

        configuration.GetRequiredSection("Identity").Bind(options);
    }
}
