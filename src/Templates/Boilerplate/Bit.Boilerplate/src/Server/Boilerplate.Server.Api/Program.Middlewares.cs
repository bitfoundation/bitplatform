//+:cnd:noEmit

using Scalar.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Boilerplate.Server.Api.Features.Identity;
//#if (signalR == true)
using Boilerplate.Server.Api.Features.Attachments;
//#endif
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

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
        app.UseRateLimiter(); // After UseAuthentication, so rate limit partitions can use HttpContext.User.
        app.UseAuthorization();

        app.UseOutputCache();

        app.UseAntiforgery();

        app.MapAppHealthChecks();

        app.MapOpenApi().CacheOutput("AppResponseCachePolicy");
        app.MapScalarApiReference().CacheOutput("AppResponseCachePolicy");
        app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
        app.MapGet("/swagger", () => Results.Redirect("/scalar")).ExcludeFromDescription();

        app.UseHangfireDashboard(options: new()
        {
            DarkModeEnabled = true,
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });

        app.ScheduleAppRecurringJobs();

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
    /// Recurring hangfire jobs. AddOrUpdate is idempotent and keyed by job id, so re-running it on every start (and
    /// on every replica) simply re-applies the current schedule.
    /// </summary>
    public static WebApplication ScheduleAppRecurringJobs(this WebApplication app)
    {
        var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<UserSessionsRetentionJobRunner>(UserSessionsRetentionJobRunner.RecurringJobId,
                                                                       runner => runner.EnforceRetention(CancellationToken.None),
                                                                       Cron.Daily);

        recurringJobManager.AddOrUpdate<UnconfirmedUsersRetentionJobRunner>(UnconfirmedUsersRetentionJobRunner.RecurringJobId,
                                                                           runner => runner.EnforceRetention(CancellationToken.None),
                                                                           Cron.Daily);

        //#if (notification == true)
        recurringJobManager.AddOrUpdate<PushSubscriptionsRetentionJobRunner>(PushSubscriptionsRetentionJobRunner.RecurringJobId,
                                                                            runner => runner.EnforceRetention(CancellationToken.None),
                                                                            Cron.Daily);
        //#endif

        //#if (signalR == true)
        // Hourly rather than daily: the retention period is measured in hours (See AiChatImagesRetention), so a daily
        // sweep would keep an expired picture for most of a day after it was due to go.
        recurringJobManager.AddOrUpdate<AiChatImagesRetentionJobRunner>(AiChatImagesRetentionJobRunner.RecurringJobId,
                                                                       runner => runner.EnforceRetention(CancellationToken.None),
                                                                       Cron.Hourly);
        //#endif

        return app;
    }

    /// <summary>
    /// This allows other backends to retrieve the OpenID Connect configuration and the public key for validating JWT tokens issued by this server.
    /// Checkout AppCertificate.md for more information.
    /// </summary>
    public static WebApplication MapOpenIdConfiguration(this WebApplication app)
    {
        var jwks = AppCertificateService.GetPublicSecurityKeys(app.Configuration)
            .Select(publicKey =>
            {
                var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
                jwk.Use = "sig";
                jwk.Alg = SecurityAlgorithms.RsaSha256;
                return jwk;
            })
            .ToArray();

        app.MapGet("/.well-known/openid-configuration", (HttpRequest request) =>
        {
            var baseUrl = request.GetBaseUrl();
            return new
            {
                issuer = app.Configuration["Identity:Issuer"],
                jwks_uri = new Uri(baseUrl, ".well-known/jwks"),
            };
        });

        app.MapGet("/.well-known/jwks", () =>
        {
            return new
            {
                keys = jwks
            };
        });

        return app;
    }
}
