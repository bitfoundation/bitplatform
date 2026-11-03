using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
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

            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = section.GetValue<ForwardedHeaders?>(nameof(ForwardedHeadersOptions.ForwardedHeaders)) ?? ForwardedHeaders.All,
                ForwardLimit = section.GetValue<int?>(nameof(ForwardedHeadersOptions.ForwardLimit)) ?? 2
            };

            foreach (var host in section.GetSection(nameof(ForwardedHeadersOptions.AllowedHosts)).Get<string[]>() ?? [])
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
            var csp = string.Join(' ',
                "default-src 'self';",
                "script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval';",
                "style-src 'self' 'unsafe-inline';",
                "img-src 'self' data: blob: https:;",
                "font-src 'self';",
                "connect-src 'self' wss: https:;",
                "frame-src https://www.openstreetmap.org;",
                "object-src 'none';",
                "frame-ancestors 'self';",
                "form-action 'self';",
                "worker-src 'self';",
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
                    else if (context.Response.StatusCode is StatusCodes.Status200OK && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) is true)
                    {
                        headers.CacheControl = new() { Public = true, MaxAge = TimeSpan.Zero, SharedMaxAge = TimeSpan.FromMinutes(10) };
                    }

                    return Task.CompletedTask;
                });

                await next.Invoke();
            });

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
                && context.Request.Path.StartsWithSegments("/_framework") is false
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
