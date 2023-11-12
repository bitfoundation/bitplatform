//-:cnd:noEmit
#if BlazorServer
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using TodoTemplate.Client.Core.Services.HttpMessageHandlers;
using TodoTemplate.Client.Web.Services;

namespace TodoTemplate.Client.Web.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(sp =>
        {
            Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);
            var handler = sp.GetRequiredService<LocalizationDelegatingHandler>();
            HttpClient httpClient = new(handler)
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
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);
        services.AddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.AddSharedServices();
        services.AddClientSharedServices();
        services.AddClientWebServices();
    }
}
#endif
