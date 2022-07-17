#if BlazorServer
using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Platform.WebSite.Web.Startup;

public class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddPlaygroundServices();

        services.AddHttpContextAccessor();

        services.AddHttpClient("ApiHttpClient", (serviceProvider, httpClient) =>
        {
            httpClient.BaseAddress = new Uri(serviceProvider.GetRequiredService<IConfiguration>()["ApiServerAddress"]);
        });
        services.AddTransient(c => c.GetRequiredService<IHttpClientFactory>().CreateClient("ApiHttpClient"));
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }).ToArray();
                opts.Providers.Add<BrotliCompressionProvider>();
                opts.Providers.Add<GzipCompressionProvider>();
            })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);
    }
}
#endif
