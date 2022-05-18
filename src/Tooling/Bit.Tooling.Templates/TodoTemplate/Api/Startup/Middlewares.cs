using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;

namespace TodoTemplate.Api.Startup
{
    public class Middlewares
    {
        public static void Use(IApplicationBuilder app, IHostEnvironment env)
        {
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
            
            app.UseResponseCompression();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
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

            app.UseCors(options => options.WithOrigins("https://localhost:4001", "https://0.0.0.0").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

            app.UseResponseCaching();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization();

#if BlazorWebAssembly
                endpoints.MapFallbackToPage("/_Host");
#endif
            });
        }
    }
}
