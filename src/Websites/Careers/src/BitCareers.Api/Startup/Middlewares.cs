using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;

namespace BitCareers.Api.Startup;

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

        app.UseCors(options => options.WithOrigins("https://localhost:4001").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

        app.UseResponseCaching();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.InjectJavascript("/swagger/swagger-utils.js");
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();


#if BlazorWebAssembly
            endpoints.MapFallbackToPage("/_Host");
#endif
        });
    }
}
