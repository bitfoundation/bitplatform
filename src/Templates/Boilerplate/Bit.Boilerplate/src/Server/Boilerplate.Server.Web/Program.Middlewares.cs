//+:cnd:noEmit
using System.Net;
using System.Web;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Components.Endpoints;
using Boilerplate.Client.Core.Services;

namespace Boilerplate.Server.Web;

public static partial class Program
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
    /// </summary>
    private static void ConfiureMiddlewares(this WebApplication app)
    {
        var configuration = app.Configuration;
        var env = app.Environment;

        app.UseForwardedHeaders();

        if (CultureInfoManager.MultilingualEnabled)
        {
            var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).ToArray();
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            }.SetDefaultCulture(CultureInfoManager.DefaultCulture.Name));
        }

        app.UseExceptionHandler("/", createScopeForErrors: true);

        if (env.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseResponseCaching();
            app.UseResponseCompression();
        }

        Configure_401_403_404_Pages(app);

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (env.IsDevelopment() is false)
                {
                    // https://bitplatform.dev/templates/cache-mechanism
                    ctx.Context.Response.GetTypedHeaders().CacheControl = new()
                    {
                        MaxAge = TimeSpan.FromDays(7),
                        Public = true
                    };
                }
            }
        });

        if (string.IsNullOrEmpty(env.WebRootPath) is false && Path.Exists(Path.Combine(env.WebRootPath, @".well-known")))
        {
            // https://yurl.chayev.com/
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, @".well-known")),
                RequestPath = new PathString("/.well-known"),
                DefaultContentType = "application/json",
                ServeUnknownFileTypes = true
            });
        }

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.MapGet("/api/minimal-api-sample/{routeParameter}", (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test");

        app.MapControllers().RequireAuthorization();

        // Handle the rest of requests with blazor
        var blazorApp = app.MapRazorComponents<Components.App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Boilerplate") is true).Except([Assembly.GetExecutingAssembly()]).ToArray());

        if (AppRenderMode.PrerenderEnabled is false)
        {
            blazorApp.AllowAnonymous(); // Server may not check authorization for pages when there's no pre rendering, let the client handle it.
        }
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
                if (context.Request.Path.Value.Contains("not-found", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                if (context.Request.Path.Value.Contains("not-authorized", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.StatusCode = context.Request.Query["isForbidden"].FirstOrDefault() is "true" ? (int)HttpStatusCode.Forbidden : (int)HttpStatusCode.Unauthorized;
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

                    var qs = HttpUtility.ParseQueryString(httpContext.Request.QueryString.Value ?? string.Empty);
                    qs.Remove("try_refreshing_token");
                    var returnUrl = UriHelper.BuildRelative(httpContext.Request.PathBase, httpContext.Request.Path, new QueryString(qs.ToString()));
                    httpContext.Response.Redirect($"/not-authorized?return-url={returnUrl}&isForbidden={(is403 ? "true" : "false")}");
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
}
