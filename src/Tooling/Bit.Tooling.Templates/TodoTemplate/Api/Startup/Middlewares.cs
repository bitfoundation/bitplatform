using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

                if (env.IsDevelopment())
                    app.UseDeveloperExceptionPage();

#if BlazorWebAssembly
                if (env.IsDevelopment())
                    app.UseWebAssemblyDebugging();
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
                    if (context.Request.Cookies?["access_token"] != null)
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

            if (env.IsDevelopment())
                app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

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
