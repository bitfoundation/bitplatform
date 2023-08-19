//-:cnd:noEmit
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace AdminPanel.Server.Api.Startup;

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
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(async () =>
            {
                // DLLs' compression is lost via CDNs like Cloud flare. We use 'no-transform' in the cache header,
                // ensuring the CDN returns the original, compressed response to the client.
                if (context.Response?.Headers?.ContentType.ToString() is System.Net.Mime.MediaTypeNames.Application.Octet)
                    context.Response.Headers.Append(HeaderNames.CacheControl, "no-transform");
            });

            await next.Invoke(context);
        });

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
                // https://bitplatform.dev/adminpanel/cache-mechanism
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromDays(7),
                    Public = true
                };
            }
        });

        app.UseRouting();

        app.UseCors(options => options.WithOrigins("https://localhost:4031" /*BlazorServer*/, "http://localhost:8001" /*BlazorElectron*/, "https://0.0.0.0" /*BlazorHybrid*/, "app://0.0.0.0" /*BlazorHybrid*/)
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials());

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
            options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.MapControllers().RequireAuthorization();

        var appsettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appsettings.HealthCheckSettings;

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
