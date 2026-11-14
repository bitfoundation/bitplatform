//+:cnd:noEmit
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        // [mirror] forwarded headers setup - keep in sync with:
        // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/WebApplicationExtensions.cs (UseAppForwardedHeaders)
        public WebApplication UseLandingForwardedHeaders()
        {
            var section = app.Configuration.GetSection("ForwardedHeaders");

            if (section.Exists() is false)
                return app;

            var options = new ForwardedHeadersOptions();
            options.ForwardedHeaders = section.GetValue(nameof(ForwardedHeadersOptions.ForwardedHeaders), options.ForwardedHeaders);
            options.ForwardLimit = section.GetValue(nameof(ForwardedHeadersOptions.ForwardLimit), options.ForwardLimit);
            options.ForwardedForHeaderName = section.GetValue(nameof(ForwardedHeadersOptions.ForwardedForHeaderName), options.ForwardedForHeaderName)!;
            options.ForwardedHostHeaderName = section.GetValue(nameof(ForwardedHeadersOptions.ForwardedHostHeaderName), options.ForwardedHostHeaderName)!;
            options.ForwardedProtoHeaderName = section.GetValue(nameof(ForwardedHeadersOptions.ForwardedProtoHeaderName), options.ForwardedProtoHeaderName)!;

            foreach (var host in section.GetSection(nameof(ForwardedHeadersOptions.AllowedHosts)).Get<string[]>() ?? [])
                options.AllowedHosts.Add(host);
            foreach (var host in app.Services.GetRequiredService<LandingServerSettings>().TrustedOrigins.Select(LandingServerSettings.GetTrustedOriginHost))
                options.AllowedHosts.Add(host);
            foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in section.GetSection("KnownIPNetworks").Get<string[]>() ?? [])
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

            if (app.Environment.IsDevelopment() || options.AllowedHosts.Any())
            {
                app.UseForwardedHeaders(options);
            }

            return app;
        }

        // [mirror] the security baseline these headers set - keep in sync with:
        // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/WebApplicationExtensions.cs (UseSecurityHeaders)
        public WebApplication UseLandingSecurityHeaders()
        {
            var otlpOrigin = app.Configuration.GetBrowserOtlpEndpoint()?.Endpoint.GetLeftPart(UriPartial.Authority);
            var telemetryOrigins = "https://*.applicationinsights.azure.com https://dc.services.visualstudio.com";
            //#if (sentry == true)
            telemetryOrigins += " https://*.sentry.io";
            if (Uri.TryCreate(app.Configuration["Logging:Sentry:Dsn"], UriKind.Absolute, out var sentryDsn) && sentryDsn.Host.EndsWith(".sentry.io", StringComparison.OrdinalIgnoreCase) is false)
            {
                telemetryOrigins += $" {sentryDsn.Scheme}://{sentryDsn.Authority}";
            }
            //#endif

            var csp = string.Join(' ',
                "default-src 'self';",
                "script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval';",
                "style-src 'self' 'unsafe-inline';",
                "img-src 'self' data: blob:;",
                "font-src 'self';",
                $"connect-src 'self' {telemetryOrigins}{(otlpOrigin is null ? null : $" {otlpOrigin}")};",
                "frame-src https://www.openstreetmap.org;",
                "object-src 'none';",
                "frame-ancestors 'self';",
                "form-action 'self';",
                "worker-src 'self';",
                "base-uri 'self';",
                "upgrade-insecure-requests;");

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;

                    if (context.Request.IsHttps && IPAddress.TryParse(context.Request.Host.Host, out var ip) is false && context.Request.Host.Host is not "localhost")
                    {
                        headers.StrictTransportSecurity = "max-age=2592000";
                    }

                    headers.XContentTypeOptions = "nosniff";
                    headers.XFrameOptions = "SAMEORIGIN";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=(), usb=(), display-capture=()";
                    headers["Cross-Origin-Opener-Policy"] = "same-origin";

                    if (headers.ContentType.ToString().StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                    {
                        headers.ContentSecurityPolicy = csp;
                    }

                    return Task.CompletedTask;
                });

                await next.Invoke();
            });

            return app;
        }

        public WebApplication UseLandingCultureUrls()
        {
            if (LandingCultures.InvariantGlobalization)
                return app;

            app.Use(async (context, next) =>
            {
                var request = context.Request;

                if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
                {
                    var path = request.Path.Value ?? "/";
                    var segments = path.Split('/', 3);

                    // One URL per page: a culture in another casing, or the default culture's prefix, redirects to the canonical path
                    if (segments.Length > 1 && LandingCultures.Find(segments[1]) is { } culture && culture.Name != segments[1] || segments.Length > 1 && segments[1] == LandingCultures.Default.Name)
                    {
                        var rest = segments.Length > 2 ? $"/{segments[2]}" : "/";
                        var target = LandingCultures.Localize(rest, LandingCultures.Find(segments[1])!);

                        context.Response.Redirect(UriHelper.BuildRelative(request.PathBase, target, request.QueryString), permanent: true);
                        return;
                    }
                }

                await next.Invoke();
            });

            return app;
        }

        // Plain HTML, since Blazor may be what failed; a 500 gets no cache header, so a CDN doesn't keep it
        public WebApplication UseLandingExceptionHandler()
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                // A bad request is the caller's fault: it isn't a 500, and it isn't worth an error in the log
                StatusCodeSelector = exception => exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError,
                SuppressDiagnosticsCallback = context => context.Exception is BadHttpRequestException,
                ExceptionHandler = async context =>
                {
                    var path = context.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? string.Empty;
                    var culture = LandingCultures.Find(path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()) ?? LandingCultures.Default;
                    LandingCultures.Apply(culture);

                    var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<LandingStrings>>();

                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync($"""
                        <!DOCTYPE html>
                        <html lang="{culture.Name}" dir="{(culture.TextInfo.IsRightToLeft ? "rtl" : "ltr")}">
                        <meta charset="utf-8" />
                        <meta name="viewport" content="width=device-width, initial-scale=1" />
                        <meta name="color-scheme" content="light dark" />
                        <title>{localizer[nameof(LandingStrings.ErrorPageTitle)]}</title>
                        <body style="display:grid;place-content:center;gap:8px;min-height:90vh;margin:0;padding:16px;text-align:center;font-family:system-ui,sans-serif">
                        <h1 style="margin:0;font-size:1.5rem">{localizer[nameof(LandingStrings.ErrorPageTitle)]}</h1>
                        <p style="margin:0">{localizer[nameof(LandingStrings.ErrorPageBody)]}</p>
                        <p><a href="">{localizer[nameof(LandingStrings.ErrorPageReload)]}</a></p>
                        </body>
                        </html>
                        """);
                }
            });

            return app;
        }

        public WebApplication UseLandingStaticFiles()
        {
            var env = app.Environment;

            // [mirror] static file cache headers - keep in sync with:
            // - src/Server/Boilerplate.Server.Web/Program.Middlewares.cs
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.GetTypedHeaders();

                    if (env.IsDevelopment())
                    {
                        headers.CacheControl = new() { NoCache = true };
                    }
                    else if (context.Request.Query.ContainsKey("v") && env.WebRootFileProvider.GetFileInfo(context.Request.Path).Exists)
                    {
                        headers.CacheControl = new() { Public = true, NoTransform = true, MaxAge = TimeSpan.FromDays(7) };
                    }
                    else if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) is true
                        && (context.Request.IsLightHouseRequest() || context.Request.IsCrawlerClient()))
                    {
                        // These callers get no scripts (See App.razor) and a CDN doesn't key on the user agent, so it must never store this page for visitors
                        headers.CacheControl = new() { Private = true, MaxAge = TimeSpan.Zero };
                    }
                    else if (context.Response.StatusCode is StatusCodes.Status200OK && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) is true)
                    {
                        // Browsers ask again on every visit, while a CDN may serve the page for ten minutes
                        headers.CacheControl = new() { Public = true, MaxAge = TimeSpan.Zero, SharedMaxAge = TimeSpan.FromMinutes(10) };
                    }

                    return Task.CompletedTask;
                });

                await next.Invoke();
            });

            // Before UseStaticFiles, which would serve the plain file and leave it to be compressed on every request
            app.UsePrecompressedStaticFiles();

            app.UseStaticFiles();

            return app;
        }

        // [mirror] precompressed static files - keep in sync with:
        // - src/Server/Boilerplate.Server.Web/Infrastructure/Extensions/WebApplicationExtensions.cs (UsePrecompressedStaticFiles)
        public WebApplication UsePrecompressedStaticFiles()
        {
            var contentTypes = new FileExtensionContentTypeProvider();
            contentTypes.Mappings[".br"] = "application/octet-stream";

            app.UseWhen(context => (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                && context.GetEndpoint()?.RequestDelegate is null
                && contentTypes.TryGetContentType(context.Request.Path.Value!, out _)
                && app.Environment.WebRootFileProvider.GetFileInfo($"{context.Request.Path}.br").Exists, precompressedApp =>
            {
                precompressedApp.Use(async (context, next) =>
                {
                    context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);

                    if (context.Request.GetTypedHeaders().AcceptEncoding.Any(encoding => encoding.Value.Equals("br", StringComparison.OrdinalIgnoreCase) && encoding.Quality is not 0))
                    {
                        context.Request.Path = $"{context.Request.Path}.br";
                        context.Response.Headers.ContentEncoding = "br";
                        context.Response.GetTypedHeaders().CacheControl = new() { NoTransform = true };
                    }

                    await next.Invoke();
                });

                precompressedApp.UseStaticFiles(new StaticFileOptions
                {
                    ContentTypeProvider = contentTypes,
                    OnPrepareResponse = file =>
                    {
                        if (file.Context.Response.Headers.ContentEncoding == "br"
                            && contentTypes.TryGetContentType(Path.GetFileNameWithoutExtension(file.File.Name), out var contentType))
                        {
                            file.Context.Response.ContentType = contentType;
                        }
                    }
                });
            });

            return app;
        }
    }
}
