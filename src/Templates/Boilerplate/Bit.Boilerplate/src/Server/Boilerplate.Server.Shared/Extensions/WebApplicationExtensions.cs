//+:cnd:noEmit
using HealthChecks.UI.Client;
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    public static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            var healthChecks = app.MapGroup("");

            healthChecks
                .CacheOutput("HealthChecks");

            // All health checks must pass for app to be
            // considered ready to accept traffic after starting
            healthChecks.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag
            // must pass for app to be considered alive
            healthChecks.MapHealthChecks("/alive", new()
            {
                Predicate = static r => r.Tags.Contains("live")
            });

            healthChecks.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        return app;
    }

    public static WebApplication UseAppForwardedHeaders(this WebApplication app)
    {
        var configuration = app.Configuration;

        ServerSharedSettings settings = new();
        configuration.Bind(settings);

        var forwardedHeadersOptions = settings.ForwardedHeaders;
        if (forwardedHeadersOptions != null)
        {
            forwardedHeadersOptions.AllowedHosts = [.. (forwardedHeadersOptions.AllowedHosts ?? []).Union(settings.TrustedOrigins.Select(origin => origin.Host))];

            if (app.Environment.IsDevelopment() || forwardedHeadersOptions.AllowedHosts.Any())
            {
                // If the list is empty then all hosts are allowed. Failing to restrict this these values may allow an attacker to spoof links generated for reset password etc.
                app.UseForwardedHeaders(forwardedHeadersOptions);
            }
        }

        return app;
    }

    public static WebApplication UseLocalization(this WebApplication app)
    {
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

        return app;
    }
}
