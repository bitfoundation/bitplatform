using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Bit.Websites.Platform.Client.Shared;
using Bit.Websites.Platform.Server.Components;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;

namespace Bit.Websites.Platform.Server.Startup;

public class Middlewares
{
    public static void Use(WebApplication app, IWebHostEnvironment env, IConfiguration configuration)
    {
        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();

            app.UseSecurityHeaders();
        }

        UseMovedDocsRedirects(app);

        Configure_404_Page(app);

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

        app.UseResponseCaching();
        app.UseRateLimiter();
        app.UseAntiforgery();

        app.UseExceptionHandler("/", createScopeForErrors: true);

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapHub<SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);

        app.MapControllers();

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks)
        {
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecksUI();
        }

        UseSiteMap(app);

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Websites.Platform") is true).Except([Assembly.GetExecutingAssembly()]).ToArray());
    }

    private static void UseMovedDocsRedirects(WebApplication app)
    {
        // These doc sections used to live on this site and are still search-indexed and linked from
        // READMEs/NuGet pages; permanent redirects transfer that ranking to their new homes instead
        // of dropping visitors on the 404 page.
        app.MapGet("/bswup", () => Results.Redirect(Urls.Bswup, permanent: true));
        app.MapGet("/bswup/{**rest}", () => Results.Redirect(Urls.Bswup, permanent: true));

        app.MapGet("/butil", () => Results.Redirect(Urls.Butil, permanent: true));
        app.MapGet("/butil/{**rest}", () => Results.Redirect(Urls.Butil, permanent: true));

        app.MapGet("/templates/samples", () => Results.Redirect(Urls.Demos, permanent: true));
        app.MapGet("/boilerplate/samples", () => Results.Redirect(Urls.Demos, permanent: true));
    }

    private static void Configure_404_Page(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.HasValue)
            {
                if (context.Request.Path.Value.Contains("not-found", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
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

                if (httpContext.Response.StatusCode is 404 &&
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
        var urls = Assembly.Load("Bit.Websites.Platform.Client")
            .ExportedTypes
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
            .Select(r => r.Template)
            .Where(t => SiteMapUrls.NoIndexUrls.Contains(t) is false
                     && SiteMapUrls.NonCanonicalUrls.Contains(t) is false
                     && t.StartsWith("/boilerplate") is false)
            .ToList();

        const string siteMapHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<urlset\r\n      xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"\r\n      xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n      xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9\r\n            http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\">";

        // Keyed by the serving host so staging/test deployments emit their own URLs, not production's.
        ConcurrentDictionary<string, string> siteMapPerHost = new();

        app.MapGet("/sitemap.xml", async context =>
        {
            var siteMap = siteMapPerHost.GetOrAdd(context.Request.GetBaseUrl(), baseUrlString =>
            {
                var baseUrl = new Uri(baseUrlString);
                return $"{siteMapHeader}{string.Join(Environment.NewLine, urls.Select(u => $"<url><loc>{new Uri(baseUrl, u)}</loc></url>"))}</urlset>";
            });

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(siteMap, context.RequestAborted);
        });
    }
}
