//+:cnd:noEmit
using Microsoft.Net.Http.Headers;

namespace Boilerplate.Api;

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

        // 0.0.0.0 origins are essential for the proper functioning of BlazorHybrid's WebView, while localhost:4030 is a prerequisite for BlazorWebAssemblyStandalone testing.
        app.UseCors(options => options.WithOrigins("https://0.0.0.0", "app://0.0.0.0", "http://localhost:4030", "http://localhost:5030")
            .AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(HeaderNames.RequestId));

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
        });

        app.MapGet("/api/minimal-api-sample/{routeParameter}", (string routeParameter, [FromQuery] string queryStringParameter) => new
        {
            RouteParameter = routeParameter,
            QueryStringParameter = queryStringParameter
        }).WithTags("Test");

        app.MapGet("/.well-known/apple-app-site-association", async () =>
        {
            // https://branch.io/resources/aasa-validator/ 
            var contentType = "application/json; charset=utf-8";
            var path = Path.Combine("wwwroot/.well-known", "apple-app-site-association");
            return Results.Stream(File.OpenRead(path), contentType, "apple-app-site-association");
        }).ExcludeFromDescription();

        app.MapControllers().RequireAuthorization();
    }
}
