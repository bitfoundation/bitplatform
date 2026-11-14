//+:cnd:noEmit
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Shared.Features.Diagnostic;
using Boilerplate.Shared.Infrastructure.Dtos;
using Boilerplate.Shared.Infrastructure.Services;
using Boilerplate.Server.Shared.Infrastructure.Services;
using Boilerplate.Server.Shared.Infrastructure.HealthChecks;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        /// <param name="healthzAuthorization">
        /// What <c>/healthz</c> requires where this app issues OAuth tokens for it (see <c>OAuthResources</c>, which
        /// this project cannot reference). Defaults to the feature alone.
        /// </param>
        public WebApplication MapAppHealthChecks(IAuthorizeData[]? healthzAuthorization = null)
        {
            var healthChecks = app.MapGroup("");

            ((IEndpointConventionBuilder)healthChecks.CacheOutput("HealthChecks"))
                .Add(endpoint =>
                {
                    if (endpoint.RequestDelegate is not { } requestDelegate) return;

                    // Keeps the checks' own spans out of the export too
                    endpoint.RequestDelegate = context =>
                    {
                        Activity.Current?.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                        return requestDelegate(context);
                    };
                });

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

            // The detailed report of the health checks page: behind its feature and never cached, as it carries each
            // failure's exception.
            HealthCheckOptions healthzOptions = new()
            {
                Predicate = _ => true,
                ResponseWriter = WriteHealthReport
            };

            foreach (var path in new[] { "/healthz", "/healthz/v1" })
            {
                app.MapHealthChecks(path, healthzOptions)
                   .RequireAuthorization(healthzAuthorization ?? [new AuthorizeAttribute(AppFeatures.System.Operations_View)]);
            }

            return app;
        }

        /// <summary>
        /// Server.Web's own settings, for the operations page of a deployment whose api is standalone: the two are
        /// separate processes. The api serves the same shape through <c>IDiagnosticController.GetDeploymentConfiguration</c>.
        /// </summary>
        public WebApplication MapDeploymentConfiguration()
        {
            app.MapGet("/deployment-configuration", (IConfiguration configuration, ServerSharedSettings settings, IHostEnvironment environment)
                => Results.Json(DeploymentConfigurationReader.ReadShared(configuration, settings, environment, Assembly.GetEntryAssembly()!),
                                AppJsonContext.Default.DeploymentConfigurationDto))
            .RequireAuthorization(AppFeatures.System.Operations_View)
            .ExcludeFromDescription();

            return app;
        }

        // [mirror] forwarded headers setup - keep in sync with:
        // - src/LandingPage/Boilerplate.LandingPage.Server/Infrastructure/Extensions/WebApplicationExtensions.cs (UseLandingForwardedHeaders)
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

            // IPAddress/IPNetwork don't bind from config, so DynamicBind drops KnownProxies/KnownIPNetworks values;
            // parse them here. Loopback (::1, 127.0.0.0/8) is trusted by default, so a localhost proxy needs none.
            foreach (var proxy in forwardedHeadersConfig.GetSection("KnownProxies").Get<string[]>() ?? [])
                forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in forwardedHeadersConfig.GetSection("KnownIPNetworks").Get<string[]>() ?? [])
                forwardedHeadersOptions.KnownIPNetworks.Add(IPNetwork.Parse(network));

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

        /// <summary>
        /// Stamps every response with a Request-Id header, so one can find its logs by it, and so the client's
        /// ExceptionDelegatingHandler can tell an error written by this app - a problem details json, or a plain
        /// localized message such as AttachmentController's image validation errors - apart from one written by
        /// something in front of it (a stopped IIS site, a reverse proxy or a CDN), whatever its status code.
        /// Written from Response.OnStarting for the same reason UseSecurityHeaders explains: UseExceptionHandler's
        /// HttpResponse.Clear() drops anything written eagerly, and the output cache never replays this header.
        /// </summary>
        public WebApplication UseRequestIdHeader()
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[HeaderNames.RequestId] = context.TraceIdentifier;
                    return Task.CompletedTask;
                });

                await next.Invoke();
            });

            return app;
        }

        // [mirror] the security baseline these headers set - keep in sync with:
        // - src/Client/Boilerplate.Client.Web/wwwroot/_headers (Cloudflare Pages)
        // - src/Client/Boilerplate.Client.Web/wwwroot/staticwebapp.config.json (Azure Static Web Apps)
        // - src/Client/Boilerplate.Client.Maui/Platforms/Android/AppWebChromeClient.cs (Permissions-Policy only)
        // - src/LandingPage/Boilerplate.LandingPage.Server/Infrastructure/Extensions/WebApplicationExtensions.cs (UseLandingSecurityHeaders, with the landing page's own Content-Security-Policy)
        // The first two serve the published wwwroot without ever running this middleware, so they repeat every header below.
        // The third grants and denies is the same decision Permissions-Policy makes here.
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
            var csp = "object-src 'none'; frame-ancestors 'self'; form-action 'self'; worker-src 'self'; base-uri 'self';";
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

                    // 3. X-Frame-Options (XFO)
                    // Prevents Clickjacking by ensuring the site can only be framed by itself (SameOrigin).
                    headers.XFrameOptions = "SAMEORIGIN";

                    // 4. Referrer-Policy
                    // Protects user privacy by only sending the origin (domain) when navigating to external sites.
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                    // 5. Permissions-Policy
                    // "Disables" sensitive hardware/API access to reduce the attack surface.
                    // Example: If building an E-Commerce or Delivery app, remove 'payment' or 'geolocation' from this list.
                    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(self), payment=(), usb=(), display-capture=()";

                    // 6. Cross-Origin-Resource-Policy (CORP)
                    // Set to 'cross-origin' to explicitly allow resources (images, fonts, etc.) to be loaded by
                    // clients on different origins/domains and Blazor Hybrid (WebView).
                    // NOTE: Using 'same-site' or 'same-origin' would block rendering in these multi-origin scenarios,
                    // but they also help prevent hotlinking and bandwidth theft from untrusted third-party sites.
                    // By choosing 'cross-origin', you allow *any* external site to embed your static assets, which can
                    // increase bandwidth costs and enable unauthorized re-use of your images/assets.
                    // Consider compensating controls such as CDN-level hotlink protection, WAF rules, rate limiting,
                    // and/or caching policies to mitigate potential abuse while still supporting hybrid/multi-origin clients.
                    headers["Cross-Origin-Resource-Policy"] = "cross-origin";

                    // 7. Content-Security-Policy (CSP)
                    if (headers.ContainsKey(HeaderNames.ContentSecurityPolicy) is false)
                    {
                        headers.ContentSecurityPolicy = csp;
                    }
                    // For a stricter, app-wide CSP that also works in all Interactive Blazor rendering modes,
                    // check out the ContentSecurityPolicy.razor component in Client.Core

                    if (app.Environment.IsProduction() is false)
                    {
                        headers["X-Robots-Tag"] = "noindex, nofollow";
                    }

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

    /// <summary>The entry assembly's, which a Release build ends with its commit (See HealthReportDto.ApplicationVersion).</summary>
    private static readonly string? ApplicationInformationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    private static readonly DateTimeOffset ProcessStartedAt = ReadProcessStartedAt();

    private static DateTimeOffset ReadProcessStartedAt()
    {
        using var process = Process.GetCurrentProcess();

        return process.StartTime.ToUniversalTime();
    }

    private static Task WriteHealthReport(HttpContext context, HealthReport report)
    {
        var registrations = context.RequestServices.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ToDictionary(r => r.Name);
        var descriptions = context.RequestServices.GetRequiredService<IOptions<HealthCheckDescriptions>>().Value;

        // An exception can carry a server name or a connection string. The app's own session (the Operations page) gets
        // it; an OAuth token, which some other app holds, never does.
        var includeExceptions = context.User.HasClaim(claim => claim.Type is AppClaimTypes.OAUTH_CLIENT_ID) is false;

        HealthReportDto body = new()
        {
            Status = (HealthCheckStatus)report.Status,
            TotalDuration = report.TotalDuration,
            CheckedAt = context.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow(),
            ApplicationVersion = ApplicationInformationalVersion,
            InstanceName = System.Environment.MachineName,
            StartedAt = ProcessStartedAt,
            Entries = report.Entries.ToDictionary(entry => entry.Key, entry =>
            {
                var registration = registrations.GetValueOrDefault(entry.Key);
                var data = entry.Value.Data;
                var category = entry.Value.Tags.FirstOrDefault(tag => tag.StartsWith(HealthCheckCategories.TagPrefix, StringComparison.Ordinal))?[HealthCheckCategories.TagPrefix.Length..] is { Length: > 0 } tagged
                    ? tagged
                    : null;
                var description = descriptions.GetValueOrDefault(entry.Key);

                return new HealthReportEntryDto
                {
                    Status = (HealthCheckStatus)entry.Value.Status,
                    FailureStatus = (HealthCheckStatus)(registration?.FailureStatus ?? HealthStatus.Unhealthy),
                    Timeout = registration?.Timeout is { } timeout && timeout != Timeout.InfiniteTimeSpan ? timeout : null,
                    // A check that throws gets its exception's message as its description, which is the error text again.
                    Description = includeExceptions || entry.Value.Exception is null || entry.Value.Description != entry.Value.Exception.Message
                        ? entry.Value.Description
                        : null,
                    // A reused result's own run, not this report's cache lookup (See CachedHealthCheck).
                    Duration = data.GetValueOrDefault(HealthCheckResultData.Duration) as TimeSpan? ?? entry.Value.Duration,
                    VerifiedAt = data.GetValueOrDefault(HealthCheckResultData.VerifiedAt) as DateTimeOffset?,
                    CacheDuration = data.GetValueOrDefault(HealthCheckResultData.CacheDuration) as TimeSpan?,
                    // ToString() rather than the message alone: the inner exception is usually the one naming the cause.
                    Exception = includeExceptions ? entry.Value.Exception?.ToString() : null,
                    Tags = [.. entry.Value.Tags],
                    Category = category,
                    Title = description?.Title ?? entry.Key,
                    IconName = description?.IconName ?? HealthCheckCategories.IconOf(category),
                    About = description?.About,
                    IsNotConfigured = entry.Value.Tags.Contains(HealthCheckTags.NotConfigured),
                    // Stringified, as a check may put anything in here.
                    Data = data.Where(item => HealthCheckResultData.Keys.Contains(item.Key) is false).ToDictionary(item => item.Key, item => item.Value switch
                    {
                        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                        _ => item.Value?.ToString()
                    })
                };
            })
        };

        return context.Response.WriteAsJsonAsync(body, AppJsonContext.Default.HealthReportDto, cancellationToken: context.RequestAborted);
    }
}
