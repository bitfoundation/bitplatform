//+:cnd:noEmit

using Microsoft.AspNetCore.Localization.Routing;

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
        var forwardedHeadersOptions = settings.ForwardedHeaders;

        if (forwardedHeadersOptions is not null
            && (app.Environment.IsDevelopment() || forwardedHeadersOptions.AllowedHosts.Any()))
        {
            // If the list is empty then all hosts are allowed. Failing to restrict this these values may allow an attacker to spoof links generated for reset password etc.
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).ToArray();
            var options = new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            };
            options.SetDefaultCulture(CultureInfoManager.DefaultCulture.Name);
            options.RequestCultureProviders.Insert(1, new RouteDataRequestCultureProvider() { Options = options });
            app.UseRequestLocalization(options);
        }

        app.UseExceptionHandler();

        if (env.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseHsts();
            app.UseXContentTypeOptions();
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseXfo(options => options.SameOrigin());
        }

        if (env.IsDevelopment())
        {
            app.UseDirectoryBrowser();
        }

        app.UseStaticFiles();

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.UseAntiforgery();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript($"/scripts/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.UseHangfireDashboard(options: new()
        {
            DarkModeEnabled = true,
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });

        if (settings.SupportedAppVersions != null)
        {
            app.UseForceUpdateMiddleware(settings.SupportedAppVersions);
        }

        app.MapGet("/api/minimal-api-sample/{routeParameter}", [AppResponseCache(MaxAge = 3600 * 24)] (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test").CacheOutput("AppResponseCachePolicy");

        //#if (signalR == true)
        app.MapHub<SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);
        //#endif

        app.MapControllers()
           .RequireAuthorization()
           .CacheOutput("AppResponseCachePolicy");
    }

    public static WebApplication UseForceUpdateMiddleware(this WebApplication app, SupportedAppVersionsOptions settings)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-App-Version", out var appVersionHeaderValue)
                && appVersionHeaderValue.Any())
            {
                var appVersion = appVersionHeaderValue.Single()!;
                var appPlatformType = Enum.Parse<AppPlatformType>(context.Request.Headers["X-App-Platform"].Single()!);
                var minimumSupportedVersion = settings.GetMinimumSupportedAppVersion(appPlatformType);
                if (minimumSupportedVersion != null && Version.Parse(appVersion) < Version.Parse(minimumSupportedVersion))
                    throw new ClientNotSupportedException();
            }

            await next(context);
        });

        return app;
    }
}
