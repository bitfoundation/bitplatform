#if BlazorServer
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
#endif
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;

namespace Bit.Client.Web.BlazorUI.Playground.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddPlaygroundServices();
#if BlazorServer
            services.AddHttpClient("ApiHttpClient", (serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(serviceProvider.GetRequiredService<IConfiguration>()["ApiServerAddress"]);
            });
            services.AddTransient(c => c.GetRequiredService<IHttpClientFactory>().CreateClient("ApiHttpClient"));
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
                opts.Providers.Add<BrotliCompressionProvider>();
                opts.Providers.Add<GzipCompressionProvider>();
            })
                .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
                .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);
#endif
        }

#if BlazorServer
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
#endif
    }
}
