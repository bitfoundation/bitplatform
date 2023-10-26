using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace Bit.BlazorUI.Demo.Server.Api.Startup;

public class Middlewares
{
    public static void Use(WebApplication app, IHostEnvironment env, IConfiguration configuration)
    {
        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

#if BlazorWebAssembly
            if (env.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
#endif
        }

#if BlazorWebAssembly
        app.UseBlazorFrameworkFiles();
#endif

        if (env.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // https://bitplatform.dev/templates/cache-mechanism
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromDays(7),
                    Public = true
                };
            }
        });

        app.UseRouting();

        // 0.0.0.0 is for the Blazor Hybrid mode (Android, iOS, Windows apps)
        app.UseCors(options => options.WithOrigins("https://localhost:4001", "https://0.0.0.0", "app://0.0.0.0").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

        app.UseResponseCaching();

        app.UseHttpResponseExceptionHandler();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript("/swagger/swagger-utils.js");
        });

        app.MapControllers();

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks)
        {
            app.MapHealthChecks("/healthz", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecksUI();
        }

#if BlazorWebAssembly
        app.MapFallbackToPage("/_Host");
#endif
    }
}
