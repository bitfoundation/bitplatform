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
                app.UseSwagger();

                app.UseSwaggerUI();

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

            app.UseResponseCompression();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        MaxAge = TimeSpan.FromDays(365),
                        Public = true
                    };
                }
            });

            app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    if (context.Request.Query["access_token"].Any())
                    {
                        context.Request.Headers.Add("Authorization", new[]
                        {
                            $"{JwtBearerDefaults.AuthenticationScheme} {context.Request.Query["access_token"]}"
                        });
                    }
                    else if (context.Request.Cookies?["access_token"] != null)
                    {
                        context.Request.Headers.Add("Authorization", new[]
                        {
                            $"{JwtBearerDefaults.AuthenticationScheme} {context.Request.Cookies["access_token"]}"
                        });
                    }
                }

                await next();
            });

            app.UseRouting();

            app.UseCors(options => options.WithOrigins("https://localhost:4001", "https://0.0.0.0").AllowAnyHeader().AllowAnyMethod().AllowCredentials());

            app.UseResponseCaching();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute().RequireAuthorization();

#if BlazorWebAssembly
                endpoints.MapFallbackToPage("/_Host");
#endif
            });
        }
    }
}
