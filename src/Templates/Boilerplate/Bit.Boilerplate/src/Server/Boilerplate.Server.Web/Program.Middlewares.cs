//+:cnd:noEmit
using System.Net;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Components.Endpoints;
//#if (api == "Integrated")
using Hangfire;
using Scalar.AspNetCore;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.RequestPipeline;
//#endif

namespace Boilerplate.Server.Web;

public static partial class Program
{
    extension(WebApplication app)
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0#middleware-order
        /// </summary>
        public void ConfigureMiddlewares()
        {
            var configuration = app.Configuration;
            var env = app.Environment;

            var settings = app.Services.GetRequiredService<ServerWebSettings>();

            app.UseAppForwardedHeaders();

            app.UseLocalization();

            //#if (api == "Integrated")
            app.UseExceptionHandler();
            //#endif

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

            app.Handle40XStatusCodes();

            if (env.IsDevelopment())
            {
                app.UseDirectoryBrowser();
            }

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(async () =>
                {
                    if (env.IsDevelopment())
                    {
                        var cacheControl = context.Response.GetTypedHeaders().CacheControl ?? new();
                        cacheControl.NoCache = true;
                        context.Response.GetTypedHeaders().CacheControl = cacheControl;
                    }
                    else
                    {
                        // Caching static files on the Browser and CDN's edge servers.
                        if (context.Request.Query.Any(q => string.Equals(q.Key, "v", StringComparison.InvariantCultureIgnoreCase))
                            && env.WebRootFileProvider.GetFileInfo(context.Request.Path).Exists)
                        {
                            context.Response.GetTypedHeaders().CacheControl = new()
                            {
                                Public = true,
                                NoTransform = true,
                                MaxAge = TimeSpan.FromDays(7)
                            };
                        }
                    }
                });

                await next.Invoke();
            });

            app.UseStaticFiles();

            // https://yurl.chayev.com/
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/.well-known"), wellKnownApp =>
            {
                wellKnownApp.UseStaticFiles(new StaticFileOptions()
                {
                    FileProvider = env.WebRootFileProvider,
                    DefaultContentType = "application/json",
                    ServeUnknownFileTypes = true
                });
            });

            //#if (api == "Integrated")
            app.UseCors();
            app.UseMiddleware<ForceUpdateMiddleware>();
            //#endif

            app.UseAuthentication();
            //#if (api == "Integrated")
            app.UseRateLimiter(); // After UseAuthentication, so rate limit partitions can use HttpContext.User.
            //#endif
            app.UseAuthorization();

            app.UseCultureUrlRedirection();

            app.UseOutputCache();

            app.UseAntiforgery();

            app.MapAppHealthChecks();

            //#if (api == "Integrated")
            app.MapOpenApi().CacheOutput("AppResponseCachePolicy");
            app.MapScalarApiReference().CacheOutput("AppResponseCachePolicy");
            app.MapGet("/swagger", () => Results.Redirect("/scalar")).ExcludeFromDescription();

            app.UseHangfireDashboard(options: new()
            {
                DarkModeEnabled = true,
                Authorization = [new HangfireDashboardAuthorizationFilter()]
            });

            app.MapGet("/api/minimal-api-sample/{routeParameter}", [AppResponseCache(MaxAge = 3600 * 24)] (string routeParameter, [FromQuery] string queryStringParameter) => new
            {
                RouteParameter = routeParameter,
                QueryStringParameter = queryStringParameter
            }).WithTags("Test").CacheOutput("AppResponseCachePolicy").ExcludeFromDescription();

            //#if (signalR == true)
            if (string.IsNullOrWhiteSpace(configuration["Azure:SignalR:ConnectionString"]) is false
                && settings.WebAppRender.BlazorMode is not BlazorWebAppMode.BlazorWebAssembly)
            {
                // Azure SignalR is going to send blazor server / auto messages to the Azure Cloud which is useless in this case,
                // because scale out lots of messages that are related to the current opened tab of browser only is not necessary and will cost you lots of money.
                // https://github.com/Azure/azure-signalr/issues/1738
                // Solutions:
                // - Switch to Blazor WebAssembly in production. Hint: To leverage Blazor server's enhanced development experience in local dev environment, you can disable Azure SignalR by setting "Azure:SignalR:ConnectionString" to null in appsettings.json or appsettings.Development.json.
                // OR
                // - Use Standalone API mode:
                //    Publish and run the Server.Api project independently to serve restful APIs and SignalR services like AppHub (Just like https://adminpanel-api.bitplatform.dev/scalar deployment)
                //    and use the Server.Web project solely as a Blazor Server or pre-rendering service provider.
                throw new InvalidOperationException("Azure SignalR is not supported with Blazor Server and Auto");
            }
            app.MapHub<Api.Infrastructure.SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);
            app.MapMcp("/mcp").RequireAuthorization(); // Chatbot tools. Isolated from /dev-mcp.
                                                       //#endif

            // Both policies, so both must pass: /dev-mcp is for global admins who have turned 2FA on, not either-or.
            app.MapMcp("/dev-mcp").RequireAuthorization(AppFeatures.System.DevMcp, AuthPolicies.TFA_ENABLED);

            app.MapOpenIdConfiguration();

            app.MapControllers()
               .RequireAuthorization()
               .CacheOutput("AppResponseCachePolicy");
            //#endif

            app.UseSiteMap();

            // Handle the rest of requests with blazor
            var blazorApp = app.MapRazorComponents<Components.App>()
                .CacheOutput("AppResponseCachePolicy")
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("Boilerplate.Client") is true).ToArray());

            if (settings.WebAppRender.RenderMode is not null && settings.WebAppRender.PrerenderEnabled is false)
            {
                // In the interactive modes with pre-rendering off, nothing of the page is produced on the server -
                // blazor emits only a marker comment and the client renders everything - so endpoint authorization has
                // nothing to protect here and the client handles it.
                blazorApp.AllowAnonymous();
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
        private void Handle40XStatusCodes()
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue)
                {
                    if (context.Request.Path.Value.Contains(PageUrls.NotFound, StringComparison.InvariantCultureIgnoreCase))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                    if (context.Request.Path.Value.Contains(PageUrls.NotAuthorized, StringComparison.InvariantCultureIgnoreCase))
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

                        var qs = AppQueryStringCollection.Parse(httpContext.Request.QueryString.Value ?? string.Empty);
                        qs.Remove("try_refreshing_token");
                        var returnUrl = UriHelper.BuildRelative(httpContext.Request.PathBase, httpContext.Request.Path,
                                                                QueryString.Create(qs.Select(kv => KeyValuePair.Create(kv.Key, kv.Value?.ToString()))));
                        // return-url has to be encoded as a single value: interpolating it raw would let its inner '&'
                        // separators split into extra outer parameters and truncate the url SignIn navigates back to.
                        var redirectQuery = QueryString.Create(new KeyValuePair<string, string?>[]
                        {
                            new("return-url", returnUrl),
                            new("isForbidden", is403 ? "true" : "false")
                        });
                        httpContext.Response.Redirect($"{PageUrls.NotAuthorized}{redirectQuery}");
                    }
                    else if (httpContext.Response.StatusCode is 404 &&
                        httpContext.GetEndpoint() is null /* Please be aware that certain endpoints, particularly those associated with web API actions, may intentionally return a 404 error. */)
                    {
                        httpContext.Response.Redirect($"{PageUrls.NotFound}{QueryString.Create("url", httpContext.Request.GetEncodedPathAndQuery())}");
                    }
                }
            });
        }
    }
}
