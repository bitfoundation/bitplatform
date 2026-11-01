using Microsoft.AspNetCore.Localization;

namespace Boilerplate.Workbench;

/// <summary>
/// Builds the Workbench's request pipeline: localization, static files, the localhost-only review endpoints and the pages.
/// </summary>
public static partial class Program
{
    extension(WebApplication app)
    {
        public void ConfigureMiddlewares()
        {
            if (app.Environment.IsDevelopment() is false)
            {
                app.UseExceptionHandler("/", createScopeForErrors: true);
                app.UseHsts();
            }

            if (CultureInfoManager.InvariantGlobalization is false)
            {
                var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).ToArray();
                var options = new RequestLocalizationOptions
                {
                    ApplyCurrentCultureToResponseHeaders = true,
                    SupportedCultures = supportedCultures,
                    SupportedUICultures = supportedCultures
                }.SetDefaultCulture(CultureInfoManager.DefaultCulture.Name);

                // A preview takes its culture from its query string, so every view shows the language it was set to.
                options.RequestCultureProviders =
                [
                    new CustomRequestCultureProvider(context => Task.FromResult(GetPreviewCulture(context.Request))),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                ];

                app.UseRequestLocalization(options);
            }

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.UseWebSockets();

            // A capture turned away because too many already wait for the headless browser answers 503.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (WorkbenchCapturesBusyException) when (context.Response.HasStarted is false)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                }
            });

            // The agent's endpoints answer only requests made on this machine; anyone else gets a 404.
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/review") || context.Request.Path.StartsWithSegments("/mcp"), local => local.Use(async (context, next) =>
            {
                if (WorkbenchLocalRequests.IsLocal(context) is false)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next(context);
            }));

            app.MapWorkbenchEndpoints();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode();
        }
    }

    /// <summary>
    /// The culture in the query string of a preview, or of its Blazor circuit; null leaves the culture to the other providers.
    /// </summary>
    private static ProviderCultureResult? GetPreviewCulture(HttpRequest request)
    {
        var isPreview = request.Path.StartsWithSegments("/preview");

        if (isPreview is false && request.Path.StartsWithSegments("/_blazor") is false)
            return null;

        var culture = CultureInfoManager.GetCultureInfo(request.Query["culture"].ToString());

        if (culture is null)
            return isPreview ? new ProviderCultureResult(CultureInfoManager.DefaultCulture.Name) : null;

        return new ProviderCultureResult(culture.Name);
    }
}
