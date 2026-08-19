using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Bit.BlazorUI.Demo.Server.Components;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;

namespace Bit.BlazorUI.Demo.Server.Startup;

public class Middlewares
{
    public static void Use(WebApplication app, IWebHostEnvironment env, IConfiguration configuration)
    {
        app.UseForwardedHeaders();

        // Cross-origin isolation, required for the multi-threaded WebAssembly runtime
        // (WasmEnableThreads in Bit.BlazorUI.Demo.Client.Web) to use SharedArrayBuffer.
        // Without these headers on the top-level document, crossOriginIsolated stays
        // false and the threaded runtime refuses to start ("SharedArrayBuffer is not
        // enabled on this page"). COEP 'credentialless' is preferred over 'require-corp'
        // because it lets cross-origin subresources (fonts, images, scripts) load without
        // their own CORP/CORS headers. Safari (WebKit, i.e. every browser on iOS) does not
        // understand 'credentialless' and treats it as 'unsafe-none', so it never becomes
        // cross-origin isolated and the runtime fails to boot; for WebKit the stricter
        // 'require-corp' is sent instead. Under 'require-corp' every cross-origin
        // subresource must either carry a Cross-Origin-Resource-Policy header or be
        // loaded in CORS mode (crossorigin="anonymous"), which is why the demo pages use
        // local images and the Extras/Legacy script loaders request CORS mode.
        app.Use(async (context, next) =>
        {
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            context.Response.Headers["Cross-Origin-Embedder-Policy"] = IsWebKit(context.Request) ? "require-corp" : "credentialless";
            context.Response.OnStarting(() =>
            {
                // The COEP value above is User-Agent dependent, and COEP is read from every response
                // that establishes an embedder policy: the top-level document *and* the worker scripts
                // the threaded runtime spawns from _framework (a worker whose script response declares
                // a policy incompatible with its owner document is rejected). So "Vary: User-Agent" is
                // appended to every response, not only to HTML: without it a shared cache/CDN could
                // hand a Safari client a response cached from a Chromium request, whose 'credentialless'
                // WebKit parses as 'unsafe-none', making the worker incompatible with the document's
                // 'require-corp' and the threaded runtime fail to boot - the exact failure this
                // middleware exists to prevent. The cost is a per-User-Agent cache fragmentation of the
                // static assets, which is preferred over serving one browser the other's COEP value.
                context.Response.Headers.Append("Vary", "User-Agent");
                return Task.CompletedTask;
            });
            await next.Invoke(context);
        });

        if (env.IsDevelopment())
        {
#if INCLUDE_WASM
            app.UseWebAssemblyDebugging();
#endif
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseSecurityHeaders();
        }

        Configure_401_403_404_Pages(app);

        if (env.IsDevelopment() is false)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Query.Any(q => string.Equals(q.Key, "v", StringComparison.InvariantCultureIgnoreCase)) &&
                    env.WebRootFileProvider.GetFileInfo(context.Request.Path).Exists)
                {
                    context.Response.OnStarting(async () =>
                    {
                        context.Response.GetTypedHeaders().CacheControl = new()
                        {
                            Public = true,
                            NoTransform = true,
                            MaxAge = TimeSpan.FromDays(7)
                        };
                    });
                }
                await next.Invoke();
            });
        }
        app.UseStaticFiles();

        app.UseCors(options => options.WithOrigins("https://0.0.0.0", "https://0.0.0.1" /*BlazorHybrid*/, "app://0.0.0.0", "app://0.0.0.1" /*BlazorHybrid*/)
            .AllowAnyHeader().AllowAnyMethod());

        app.UseResponseCaching();
        app.UseAntiforgery();

        app.UseExceptionHandler("/", createScopeForErrors: true);

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapHub<SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);

        app.MapControllers();

        app.MapMcp("/mcp");

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks)
        {
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecksUI(options =>
            {
                options.UseRelativeApiPath =
                    options.UseRelativeResourcesPath =
                        options.UseRelativeWebhookPath = false;
            });
        }

        UseSiteMap(app);

        // Handle the rest of requests with blazor
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
#if INCLUDE_WASM
            .AddInteractiveWebAssemblyRenderMode()
#endif
            .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Bit.BlazorUI.Demo") is true).Except([Assembly.GetExecutingAssembly()]).ToArray());
    }

    /// <summary>
    /// Detects WebKit-based browsers: Safari on macOS and iOS/iPadOS, in-app WKWebViews (whose UA lacks the
    /// "Safari" token) and every third-party browser on iOS (Chrome, Firefox, Edge... all report
    /// "CriOS"/"FxiOS"/"EdgiOS" on top of AppleWebKit). Chromium-based browsers also carry "AppleWebKit" in
    /// their UA, so they are excluded by the "Chrome"/"Chromium" token (desktop Edge/Opera/Brave/Samsung all
    /// include "Chrome"); desktop Firefox does not contain "AppleWebKit" at all.
    /// </summary>
    private static bool IsWebKit(HttpRequest request)
    {
        var ua = request.Headers.UserAgent.ToString();

        if (ua.Contains("AppleWebKit", StringComparison.OrdinalIgnoreCase) is false) return false;

        return ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase) is false
            && ua.Contains("Chromium", StringComparison.OrdinalIgnoreCase) is false
            && ua.Contains("Firefox", StringComparison.OrdinalIgnoreCase) is false;
    }

    /// <summary>
    /// Prior to the introduction of .NET 8, the Blazor router effectively managed NotFound and NotAuthorized components during pre-rendering.
    /// However, the current behavior has changed, and it now exclusively returns 401, 403, and 404 status codes with an empty body response!
    /// To address this, we've implemented the UseStatusCodePages middleware to handle responses featuring 401, 403, and 404 status codes that lack a body.
    /// This middleware facilitates redirection to the appropriate not-found and not-authorized pages. Consequently, the status code for these responses becomes 302 (Found).
    /// To mitigate the challenges posed by this situation, our only recourse is to repurpose the 401, 403, and 404 status codes for
    /// not-found and not-authorized responses, at the very least.
    /// </summary>
    private static void Configure_401_403_404_Pages(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.HasValue)
            {
                // The status code is intentionally applied through Response.OnStarting (right before the headers
                // are sent) instead of being set up-front. Setting a 404/401/403 before Blazor renders the page
                // makes the framework treat the (successfully matched) not-found/not-authorized page as an unhandled
                // error: it skips flushing the rendered body and lets the UseStatusCodePages middleware re-execute
                // the pipeline on the same HttpContext. That re-execution invokes Blazor's
                // MarkAsAllowingEnhancedNavigation a second time, which calls Response.Headers.Add("blazor-enhanced-nav", ...)
                // again and throws "An item with the same key has already been added. Key: blazor-enhanced-nav".
                // Deferring the status code keeps the response at 200 during rendering, so the page is rendered and
                // flushed normally and the desired status code is still emitted to the client.
                if (context.Request.Path.Value.Contains("not-found", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return Task.CompletedTask;
                    });
                }
                if (context.Request.Path.Value.Contains("not-authorized", StringComparison.InvariantCultureIgnoreCase))
                {
                    var statusCode = context.Request.Query["isForbidden"].FirstOrDefault() is "true" ? (int)HttpStatusCode.Forbidden : (int)HttpStatusCode.Unauthorized;
                    context.Response.OnStarting(() =>
                    {
                        context.Response.StatusCode = statusCode;
                        return Task.CompletedTask;
                    });
                }
            }

            await next.Invoke(context);
        });

        app.UseStatusCodePages(options: new()
        {
            HandleAsync = async (statusCodeContext) =>
            {
                var httpContext = statusCodeContext.HttpContext;

                if (httpContext.Response.StatusCode is 401 or 403 &&
                    httpContext.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>().Any() is true /* The generation of a 401 or 403 status code is attributed to Blazor. */)
                {
                    bool is403 = httpContext.Response.StatusCode is 403;

                    httpContext.Response.Redirect($"/not-authorized?redirect-url={httpContext.Request.GetEncodedPathAndQuery()}&isForbidden={(is403 ? "true" : "false")}");
                }
                else if (httpContext.Response.StatusCode is 404 &&
                    httpContext.GetEndpoint() is null /* Please be aware that certain endpoints, particularly those associated with web API actions, may intentionally return a 404 error. */)
                {
                    httpContext.Response.Redirect($"/not-found?url={httpContext.Request.GetEncodedPathAndQuery()}");
                }
                else
                {
                    await statusCodeContext.Next.Invoke(statusCodeContext.HttpContext);
                }
            }
        });
    }

    private static void UseSiteMap(WebApplication app)
    {
        var urls = Assembly.Load("Bit.BlazorUI.Demo.Client.Core")
            .ExportedTypes
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            // Use only the first (canonical) route of each page and skip the noindex not-found page,
            // so the sitemap advertises canonical URLs only and avoids duplicate alias entries.
            .Select(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>()
                          .Select(r => r.Template)
                          .FirstOrDefault())
            .Where(template => string.IsNullOrWhiteSpace(template) is false
                            && string.Equals(template, "/not-found", StringComparison.OrdinalIgnoreCase) is false)
            // Normalize to lowercase so the advertised URLs match the canonical URLs emitted by PageOutlet.
            .Select(template => template!.Trim().ToLowerInvariant())
            .Distinct()
            // Home first, then alphabetical for a stable, readable sitemap.
            .OrderBy(template => template == "/" ? 0 : 1)
            .ThenBy(template => template, StringComparer.Ordinal)
            .ToList();

        const string siteMapHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<urlset\r\n      xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"\r\n      xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n      xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9\r\n            http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\">";

        app.MapGet("/sitemap.xml", async context =>
        {
            if (siteMap is null)
            {
                var baseUrl = context.Request.GetBaseUrl();
                var lastmod = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

                var entries = urls.Select(u =>
                {
                    var (changefreq, priority) = GetSiteMapHints(u);
                    var loc = new Uri(baseUrl, u).AbsoluteUri;
                    return $"<url><loc>{loc}</loc><lastmod>{lastmod}</lastmod><changefreq>{changefreq}</changefreq><priority>{priority}</priority></url>";
                });

                siteMap = $"{siteMapHeader}{string.Join(Environment.NewLine, entries)}</urlset>";
            }

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(siteMap, context.RequestAborted);
        });
    }

    /// <summary>
    /// Returns crawler hints (changefreq, priority) for a given canonical route so search engines can
    /// prioritize the landing/docs pages over the many individual component pages.
    /// </summary>
    private static (string changefreq, string priority) GetSiteMapHints(string route)
    {
        return route switch
        {
            "/" => ("weekly", "1.0"),
            "/overview" or "/getting-started" or "/theming" or "/iconography" => ("weekly", "0.9"),
            "/terms" => ("yearly", "0.3"),
            _ => ("monthly", "0.7")
        };
    }

    private static string? siteMap;
}
