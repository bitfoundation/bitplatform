//+:cnd:noEmit

using Scalar.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Server.Api.Infrastructure.RequestPipeline;

namespace Boilerplate.Server.Api;

public static partial class Program
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0#middleware-order
    /// </summary>
    private static void ConfigureMiddlewares(this WebApplication app)
    {
        var configuration = app.Configuration;
        var env = app.Environment;

        ServerApiSettings settings = new();
        configuration.Bind(settings);

        app.UseAppForwardedHeaders();

        app.UseLocalization();

        app.UseExceptionHandler();

        if (env.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseSecurityHeaders();
        }

        if (env.IsDevelopment())
        {
            app.UseDirectoryBrowser();
        }

        app.UseStaticFiles();

        app.UseCors();

        app.UseMiddleware<ForceUpdateMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.UseAntiforgery();

        app.MapAppHealthChecks();

        if (env.IsProduction() is false)
        {
            app.MapOpenApi().CacheOutput("AppResponseCachePolicy");
            app.MapScalarApiReference().CacheOutput("AppResponseCachePolicy");
            app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
            app.MapGet("/swagger", () => Results.Redirect("/scalar")).ExcludeFromDescription();
        }

        app.UseHangfireDashboard(options: new()
        {
            DarkModeEnabled = true,
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });

        app.MapGet("/api/minimal-api-sample/{routeParameter}", [AppResponseCache(MaxAge = 3600 * 24)] (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test").CacheOutput("AppResponseCachePolicy").ExcludeFromDescription();

        //#if (signalR == true)
        app.MapHub<Infrastructure.SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);
        app.MapMcp("/mcp").RequireAuthorization(); // Map MCP endpoints for chatbot tool
        //#endif

        app.MapOpenIdConfiguration();

        app.MapControllers()
           .RequireAuthorization()
           .CacheOutput("AppResponseCachePolicy");
    }


    /// <summary>
    /// Checkout AppCertificate.md for more information,
    /// </summary>
    private static WebApplication MapOpenIdConfiguration(this WebApplication app)
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(AppCertificateService.GetPublicSecurityKey());
        jwk.Use = "sig";
        jwk.Kid = "Boilerplate";

        app.MapGet("/.well-known/openid-configuration", (HttpRequest request) =>
        {
            var baseUrl = request.GetBaseUrl();
            return Results.Ok(new
            {
                issuer = baseUrl,
                jwks_uri = $"{baseUrl}/.well-known/jwks",
            });
        });

        return app;
    }
}
