using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using TodoTemplate.Api.Data.Context;
using TodoTemplate.Shared.Extensions;

#if BlazorWebAssembly
using System.Net.Http;
using Microsoft.AspNetCore.Components;
#endif

namespace TodoTemplate.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {

#if BlazorWebAssembly
            services.AddTodoTemplateServices();
            services.AddScoped(c =>
            {
                // this is for pre rendering of blazor client/wasm
                // Using this registration + registrations provided in Program.cs/Startup.cs of TodoTemplate.App project,
                // you can inject HttpClient and call TodoTemplate.Api api controllers in blazor pages.
                // for other usages of http client, for example calling 3rd party apis, please use services.AddHttpClient("NamedHttpClient"), then inject IHttpClientFactory and use its CreateClient("NamedHttpClient") method.
                return new HttpClient { BaseAddress = new Uri(c.GetRequiredService<NavigationManager>().BaseUri) };
            });
            services.AddRazorPages();
#endif

        services.AddCors();

        services.AddMvcCore();

        services.AddControllers();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddTodoTemplateSharedServices();

        services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.AddDbContext<TodoTemplateDbContext>(options =>
        {
            options.UseSqlServer(Configuration.GetConnectionString("SqlServerConnection"));
        });

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddAutoMapper(typeof(Startup).Assembly);

        // register backend specific services here, for example services.AddDbContext
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();

#if BlazorWebAssembly
                endpoints.MapFallbackToPage("/_Host");
#endif
            });
    }
}
