//-:cnd:noEmit
#if BlazorServer
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorDual.Web.Services.Implementations;

namespace BlazorDual.Web.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(sp =>
        {
            HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>())
            {
                BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"] ?? throw new InvalidOperationException("ApiServerAddress config could not be found")}")
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
        services.AddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.AddSharedServices();
        services.AddClientSharedServices();
    }
}
#endif
