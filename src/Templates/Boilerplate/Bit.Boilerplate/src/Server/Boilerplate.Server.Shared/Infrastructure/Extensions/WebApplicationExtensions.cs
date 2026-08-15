//+:cnd:noEmit
using Boilerplate.Server.Shared;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Boilerplate.Server.Shared.Infrastructure.Services;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public WebApplication MapAppHealthChecks()
        {
            var healthChecks = app.MapGroup("");

            healthChecks
                .CacheOutput("HealthChecks");

            // All health checks must pass for app to be
            // considered ready to accept traffic after starting
            healthChecks.MapHealthChecks("/health", new()
            {
                Predicate = _ => true
            });

            // Only health checks tagged with the "live" tag
            // must pass for app to be considered alive
            healthChecks.MapHealthChecks("/alive", new()
            {
                Predicate = static res => res.Tags.Contains("live")
            });

            if (app.Environment.IsDevelopment())
            {
                // This endpoint returns more details and must be protected by authentication and authorization in production
                // Replace outer `IsDevelopment` check with a more robust check for production readiness before exposing this endpoint publicly
                healthChecks.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    AllowCachingResponses = true,
                    // The following `IsDevelopment` check must remain in place to avoid exposing sensitive information in production
                    ResponseWriter = app.Environment.IsDevelopment() ? UIResponseWriter.WriteHealthCheckUIResponse : UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
                });
            }

            return app;
        }

        public WebApplication UseAppForwardedHeaders()
        {
            var configuration = app.Configuration;

            var forwardedHeadersConfig = configuration.GetSection("ForwardedHeaders");
            if (forwardedHeadersConfig.Exists() is false)
                return app;
            // If the ForwardedHeaders section is missing, we will not apply any forwarded headers configuration.
            // This is a security measure to prevent misconfiguration that could allow spoofing of links generated for reset password, etc.

            ServerSharedSettings settings = new();
            configuration.Bind(settings);

            var forwardedHeadersOptions = forwardedHeadersConfig.DynamicBind<ForwardedHeadersOptions>();
            forwardedHeadersOptions.AllowedHosts = [.. (forwardedHeadersOptions.AllowedHosts ?? []).Union(settings.TrustedOrigins.Select(ServerSharedSettings.GetTrustedOriginHost))];

            if (app.Environment.IsDevelopment() || forwardedHeadersOptions.AllowedHosts.Any())
            {
                // If the list is empty then all hosts are allowed. Failing to restrict this these values may allow an attacker to spoof links generated for reset password etc.
                app.UseForwardedHeaders(forwardedHeadersOptions);
            }

            return app;
        }

        public WebApplication UseLocalization()
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
                options.RequestCultureProviders.Remove(options.RequestCultureProviders.OfType<AcceptLanguageHeaderRequestCultureProvider>().Single());
                options.RequestCultureProviders.Add(new AppAcceptLanguageRequestCultureProvider { Options = options });
                options.RequestCultureProviders.Insert(1, new RouteDataRequestCultureProvider() { Options = options });
                app.UseRequestLocalization(options);
            }

            return app;
        }

        public WebApplication UseSecurityHeaders()
        {
            // NOTE: These headers represent a strong security baseline.
            // Depending on your application's requirements, you might need to relax or tighten these settings further.

            // They are all written from a single Response.OnStarting callback, and that is load-bearing:
            // UseExceptionHandler calls HttpResponse.Clear(), which drops the whole header collection, so any
            // header written eagerly on the way in is ABSENT from every response the exception handler produces -
            // including ordinary model-validation 400s, because InvalidModelStateResponseFactory throws.
            // OnStarting callbacks survive Clear(), so the baseline applies to error responses too.

            // Content-Security-Policy (CSP) - Mini Version
            // 'object-src none': Blocks legacy plugins like Flash.
            // 'frame-ancestors self': Modern replacement for X-Frame-Options.
            // 'form-action self': Restricts forms to only submit to your own domain (prevents form hijacking).
            var csp = "object-src 'none'; frame-ancestors 'self'; form-action 'self'; worker-src 'self';";
            if (app.Environment.IsDevelopment() is false)
            {
                // In production, add 'upgrade-insecure-requests' to automatically upgrade any HTTP requests to HTTPS.
                csp += " upgrade-insecure-requests;";
            }

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;

                    // 1. Strict-Transport-Security (HSTS)
                    // Enforces HTTPS connections. Emitted only over HTTPS and never for loopback, which is what
                    // ASP.NET Core's own HstsMiddleware does - a browser ignores it on http:// anyway.
                    // TIP: For "HSTS Preload", it's easier to configure it on Cloudflare CDN
                    // or your web server, rather than hardcoding the preload directive here.
                    if (context.Request.IsHttps && HstsExcludedHosts.Contains(context.Request.Host.Host) is false)
                    {
                        headers.StrictTransportSecurity = "max-age=2592000"; // 30 days, the framework default.
                    }

                    // 2. X-Content-Type-Options
                    // Prevents browsers from sniffing MIME types (stops executing text/plain as scripts).
                    headers.XContentTypeOptions = "nosniff";

                    // 3. X-XSS-Protection
                    // Legacy header. Enables the browser's built-in XSS filter in block mode.
                    headers.XXSSProtection = "1; mode=block";

                    // 4. X-Frame-Options (XFO)
                    // Prevents Clickjacking by ensuring the site can only be framed by itself (SameOrigin).
                    headers.XFrameOptions = "SAMEORIGIN";

                    // 5. Referrer-Policy
                    // Protects user privacy by only sending the origin (domain) when navigating to external sites.
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                    // 6. Permissions-Policy
                    // "Disables" sensitive hardware/API access to reduce the attack surface.
                    // Example: If building an E-Commerce or Delivery app, remove 'payment' or 'geolocation' from this list.
                    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(self), payment=(), usb=(), display-capture=()";

                    // 7. Cross-Origin-Resource-Policy (CORP)
                    // Set to 'cross-origin' to explicitly allow resources (images, fonts, etc.) to be loaded by
                    // clients on different origins/domains and Blazor Hybrid (WebView).
                    // NOTE: Using 'same-site' or 'same-origin' would block rendering in these multi-origin scenarios,
                    // but they also help prevent hotlinking and bandwidth theft from untrusted third-party sites.
                    // By choosing 'cross-origin', you allow *any* external site to embed your static assets, which can
                    // increase bandwidth costs and enable unauthorized re-use of your images/assets.
                    // Consider compensating controls such as CDN-level hotlink protection, WAF rules, rate limiting,
                    // and/or caching policies to mitigate potential abuse while still supporting hybrid/multi-origin clients.
                    headers["Cross-Origin-Resource-Policy"] = "cross-origin";

                    // 8. Content-Security-Policy (CSP)
                    headers.ContentSecurityPolicy = csp;
                    // For a stricter, app-wide CSP that also works in all Interactive Blazor rendering modes,
                    // check out the ContentSecurityPolicy.razor component in Client.Core

                    return Task.CompletedTask;
                });

                await next();
            });

            return app;
        }
    }

    /// <summary>
    /// The hosts ASP.NET Core's own <c>HstsMiddleware</c> excludes by default. Kept so the hand-written
    /// Strict-Transport-Security below behaves exactly like <c>UseHsts()</c> did.
    /// </summary>
    private static readonly HashSet<string> HstsExcludedHosts = new(["localhost", "127.0.0.1", "[::1]"], StringComparer.OrdinalIgnoreCase);
}
