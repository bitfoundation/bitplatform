//-:cnd:noEmit
#if BlazorServer
namespace AdminPanel.App.Startup;

public class Middlewares
{
    public static void Use(IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseHttpsRedirection();
        if (env.IsDevelopment() is false)
        {
            app.UseResponseCompression();
        }
        app.UseStaticFiles();

        app.UseRouting();

#if MultilingualEnabled
        var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => CultureInfoManager.CreateCultureInfo(sc.code)).ToArray();
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            ApplyCurrentCultureToResponseHeaders = true
        }.SetDefaultCulture(CultureInfoManager.DefaultCulture.code));
#endif

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
#endif
