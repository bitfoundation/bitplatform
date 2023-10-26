using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace Bit.Websites.Platform.Api.Startup;

public class Middlewares
{
    public static void Use(IApplicationBuilder app, IHostEnvironment env, IConfiguration configuration)
    {
        //app.Use(async (context, next) =>
        //{
        //    await Task.Delay(new Random().Next(500, 800));
        //    await next.Invoke(context);
        //});

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

        app.UseCors(options => options.WithOrigins("https://localhost:4011").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

        app.UseResponseCaching();

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

        app.UseSwaggerUI();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();

            var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

            var healthCheckSettings = appSettings.HealthCheckSettings;

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
