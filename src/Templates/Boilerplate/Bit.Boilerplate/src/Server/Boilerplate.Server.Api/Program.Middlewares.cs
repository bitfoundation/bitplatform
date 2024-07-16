﻿//+:cnd:noEmit
using Microsoft.Net.Http.Headers;

namespace Boilerplate.Server.Api;

public static partial class Program
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
    /// </summary>
    private static void ConfiureMiddlewares(this WebApplication app)
    {
        var configuration = app.Configuration;
        var env = app.Environment;

        app.UseForwardedHeaders();

        if (CultureInfoManager.MultilingualEnabled)
        {
            var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).ToArray();
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            }.SetDefaultCulture(CultureInfoManager.DefaultCulture.Name));
        }

        app.UseExceptionHandler("/", createScopeForErrors: true);

        if(env.IsDevelopment() is false)
        {
            app.UseHttpsRedirection();
            app.UseResponseCaching();
            app.UseResponseCompression();
        }

        if (env.IsDevelopment())
        {
            app.UseDirectoryBrowser();
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (env.IsDevelopment() is false)
                {
                    // https://bitplatform.dev/templates/cache-mechanism
                    ctx.Context.Response.GetTypedHeaders().CacheControl = new()
                    {
                        MaxAge = TimeSpan.FromDays(7),
                        Public = true
                    };
                }
            }
        });

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript($"/scripts/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.MapGet("/api/minimal-api-sample/{routeParameter}", (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test");

        app.MapControllers().RequireAuthorization();
    }
}
