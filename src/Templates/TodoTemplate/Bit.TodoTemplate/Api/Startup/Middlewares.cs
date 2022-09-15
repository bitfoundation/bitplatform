//-:cnd:noEmit
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace TodoTemplate.Api.Startup;

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

        if (env.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCompression();
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // https://bitplatform.dev/todo-template/cache-mechanism
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
#if PWA
                    NoCache = true
#else
                    MaxAge = TimeSpan.FromDays(365),
                    Public = true
#endif
                };
            }
        });

        app.UseRouting();

        // 0.0.0.0 is for the Blazor Hybrid mode (Android, iOS, Windows apps)
        app.UseCors(options => options.WithOrigins("https://localhost:4001", "https://0.0.0.0").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

        app.UseResponseCaching();
        app.UseAuthentication();
        app.UseAuthorization();

#if MultilingualEnabled
        var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => CultureInfoManager.CreateCultureInfo(sc.code)).ToArray();
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            ApplyCurrentCultureToResponseHeaders = true
        }.SetDefaultCulture(CultureInfoManager.DefaultCulture.code));
#endif

        app.UseHttpResponseExceptionHandler();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript("/swagger/swagger-utils.js");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();

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
