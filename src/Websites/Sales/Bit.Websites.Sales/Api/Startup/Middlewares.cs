using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace Bit.Websites.Sales.Api.Startup;

public class Middlewares
{
    public static void Use(IApplicationBuilder app, IHostEnvironment env, IConfiguration configuration)
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

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript("/swagger/swagger-utils.js");
        });

        if (env.IsDevelopment() is false)
        {
            app.UseResponseCompression();
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // https://bitplatform.dev/todo-template/cache-mechanism
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromDays(365),
                    Public = true
                };
            }
        });

        app.UseHttpResponseExceptionHandler();
        app.UseRouting();

        // 0.0.0.0 is for the Blazor Hybrid mode (Android, iOS, Windows apps)
        app.UseCors(options => options.WithOrigins("https://localhost:4001", "https://0.0.0.0").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

        app.UseResponseCaching();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            var appsettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            var healthCheckSettings = appsettings.HealthCheckSettings;

            if (healthCheckSettings.EnableHealthChecks)
            {
                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecksUI();
            }

#if BlazorWebAssembly
            endpoints.MapFallbackToPage("/_Host");
#endif
        });
    }
}
