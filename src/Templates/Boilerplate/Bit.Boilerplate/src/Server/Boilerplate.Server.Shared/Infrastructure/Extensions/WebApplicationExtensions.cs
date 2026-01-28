//+:cnd:noEmit
using Boilerplate.Server.Shared;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        // NOTE: These headers represent a strong security baseline.
        // Depending on your application's requirements, you might need to relax or tighten these settings further.

        // 1. Strict-Transport-Security (HSTS)
        // Enforces HTTPS connections.
        // TIP: For "HSTS Preload", it's easier to configure it on Cloudflare CDN
        // or your web server, rather than hardcoding the preload directive here.
        app.UseHsts();

        // 2. X-Content-Type-Options
        // Prevents browsers from sniffing MIME types (stops executing text/plain as scripts).
        app.UseXContentTypeOptions();

        // 3. X-XSS-Protection
        // Legacy header. Enables the browser's built-in XSS filter in block mode.
        app.UseXXssProtection(options => options.EnabledWithBlockMode());

        // 4. X-Frame-Options (XFO)
        // Prevents Clickjacking by ensuring the site can only be framed by itself (SameOrigin).
        app.UseXfo(options => options.SameOrigin());

        // 5. Referrer-Policy
        // Protects user privacy by only sending the origin (domain) when navigating to external sites.
        app.UseReferrerPolicy(opts => opts.StrictOriginWhenCrossOrigin());

        app.Use(async (context, next) =>
        {
            // 6. Permissions-Policy
            // "Disables" sensitive hardware/API access to reduce the attack surface.
            // Example: If building an E-Commerce or Delivery app, remove 'payment' or 'geolocation' from this list.
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=(), payment=(), usb=(), display-capture=()");

            // 7. Cross-Origin-Resource-Policy (CORP)
            // Prevents other sites from embedding your resources (Images, Scripts, etc.).
            // 'same-site' blocks Hotlinking (Bandwidth theft) from external domains but allows your subdomains.
            context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-site");

            // 8. Content-Security-Policy (CSP) - Mini Version
            // 'object-src none': Blocks legacy plugins like Flash.
            // 'frame-ancestors self': Modern replacement for X-Frame-Options.
            // 'form-action self': Restricts forms to only submit to your own domain (prevents form hijacking).
            context.Response.Headers.Append("Content-Security-Policy", "object-src 'none'; frame-ancestors 'self'; form-action 'self';");

            await next();
        });

        return app;
    }
}
