using Microsoft.Net.Http.Headers;

namespace Bit.Bswup.Demo.Server.Startup;

public static class Middlewares
{
    public static void Use(IApplicationBuilder app, IHostEnvironment env)
    {
        //app.Use(async (context, next) =>
        //{
        //    await Task.Delay(new Random().Next(500, 800));
        //    await next.Invoke(context);
        //});

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }

        app.UseBlazorFrameworkFiles();

        if (env.IsDevelopment() is false)
        {
            app.UseResponseCompression();
        }
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromDays(7),
                    Public = true
                };
            }
        });

        app.UseRouting();
        app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();

            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
