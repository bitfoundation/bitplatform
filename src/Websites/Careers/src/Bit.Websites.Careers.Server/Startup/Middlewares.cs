using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Bit.Websites.Careers.Server.Components;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;

namespace Bit.Websites.Careers.Server.Startup;

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
        }

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
        app.UseAntiforgery();

        app.UseExceptionHandler("/", createScopeForErrors: true);
        app.UseSwagger();

        app.UseSwaggerUI();

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
            .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Websites.Careers") is true).Except([Assembly.GetExecutingAssembly()]).ToArray());
    }

    private static void Configure_404_Page(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.HasValue)
            {
                if (context.Request.Path.Value.Contains("not-found", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
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
        var urls = Assembly.Load("Bit.Websites.Careers.Client")
            .ExportedTypes
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
            .Select(r => r.Template)
            .ToList();

        const string siteMapHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<urlset\r\n      xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"\r\n      xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n      xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9\r\n            http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\">";

        app.MapGet("/sitemap.xml", async context =>
        {
            if (siteMap is null)
            {
                var baseUrl = new Uri(context.Request.GetBaseUrl());

                siteMap = $"{siteMapHeader}{string.Join(Environment.NewLine, urls.Select(u => $"<url><loc>{new Uri(baseUrl, u)}</loc></url>"))}</urlset>";
            }

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(siteMap, context.RequestAborted);
        });
    }

    private static string? siteMap;
}
