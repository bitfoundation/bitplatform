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

            app.UseRouting();

            app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();

#if BlazorWebAssembly
                endpoints.MapFallbackToPage("/_Host");
#endif
            });
        }
    }
}
