//+:cnd:noEmit
using Microsoft.AspNetCore.Localization;
using Boilerplate.LandingPage.Server.Features.Pwa;
using Boilerplate.LandingPage.Server.Features.Seo;
using Boilerplate.LandingPage.Server.Features.Chat;
using Boilerplate.LandingPage.Server.Features.Waitlist;
using Boilerplate.LandingPage.Server.Features.Download;
using Boilerplate.LandingPage.Server.Features.StoreListing;

namespace Boilerplate.LandingPage.Server;

public static partial class Program
{
    extension(WebApplication app)
    {
        public void ConfigureLandingPageMiddlewares()
        {
            var env = app.Environment;

            app.UseLandingForwardedHeaders();

            //#if (framework == 'net10.0')
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            if (env.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }

            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
            //#endif

            if (env.IsDevelopment() is false)
            {
                app.UseLandingExceptionHandler();
                app.UseHttpsRedirection();
                app.UseResponseCompression();
                app.UseLandingSecurityHeaders();
            }

            app.UseLandingCultureUrls();

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                SupportedCultures = [.. LandingCultures.Supported.Select(s => s.Culture)],
                SupportedUICultures = [.. LandingCultures.Supported.Select(s => s.Culture)],
                DefaultRequestCulture = new RequestCulture(LandingCultures.Default),
                RequestCultureProviders = [new LandingUrlCultureProvider()],
                ApplyCurrentCultureToResponseHeaders = true
            });

            app.UseLandingStaticFiles();

            app.UseRouting();

            app.UseOutputCache();

            app.UseRateLimiter();

            app.UseAntiforgery();

            app.MapGet("/alive", () => Results.Ok()).ExcludeFromDescription();
            app.MapHealthChecks("/health").CacheOutput(LandingOutputCaches.Health);

            app.MapLandingSeo();
            app.MapLandingWebManifests();
            app.MapLandingDownloads();
            app.MapLandingWaitlist();
            app.MapLandingChat();
            app.MapLandingStoreListings();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client.Program).Assembly);
        }
    }
}
