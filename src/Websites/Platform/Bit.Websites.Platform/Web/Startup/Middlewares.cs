#if BlazorServer
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Bit.Websites.Platform.Web.Startup;

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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
#endif
