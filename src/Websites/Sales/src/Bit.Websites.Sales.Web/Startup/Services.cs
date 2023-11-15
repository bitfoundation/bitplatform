﻿#if BlazorServer
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Bit.Websites.Sales.Web.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>())
            {
                BaseAddress = apiServerAddress
            };

            return httpClient;
        });

        services.AddHttpContextAccessor();
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

        services.AddSharedServices();
        services.AddClientSharedServices();
    }
}
#endif
