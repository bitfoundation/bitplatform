using Microsoft.AspNetCore.Localization;

namespace Boilerplate.Workbench;

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

                options.RequestCultureProviders = [new QueryStringRequestCultureProvider(), new CookieRequestCultureProvider(), new AcceptLanguageHeaderRequestCultureProvider()];

                app.UseRequestLocalization(options);
            }

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.UseRateLimiter();

            app.MapWorkbenchEndpoints();

            app.MapRazorComponents<Components.App>()
                .AddInteractiveServerRenderMode();
        }
    }
}
