//-:cnd:noEmit
#if BlazorServer
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace TodoTemplate.App.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("BlazorServerHttpClient")
            .ConfigurePrimaryHttpMessageHandler<AppHttpClientHandler>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                httpClient.BaseAddress = new Uri($"{sp.GetRequiredService<IConfiguration>()["ApiServerAddress"]}");
            });

        services.AddScoped(sp =>
        {
            HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient("BlazorServerHttpClient");

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
        services.AddAppServices();
    }
}
#endif
