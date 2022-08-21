﻿//-:cnd:noEmit
#if BlazorServer
namespace TodoTemplate.App.Startup;

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
        var supportedCultures = new[] { "en", "fr" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);
#endif

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
#endif
