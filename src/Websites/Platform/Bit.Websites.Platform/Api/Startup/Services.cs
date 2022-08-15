using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
#if BlazorWebAssembly
using Microsoft.AspNetCore.Components;
#endif

namespace Bit.Websites.Platform.Api.Startup;

public static class Services
{
    public static void Add(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
#if BlazorWebAssembly
        services.AddScoped(c =>
        {
            // this is for pre rendering of blazor client/wasm
            // Using this registration + registrations provided in Program.cs/Startup.cs of Bit.Websites.Platform.Web project,
            // you can inject HttpClient and call Bit.Websites.Platform.Api api controllers in blazor pages.
            // for other usages of http client, for example calling 3rd party apis, please use services.AddHttpClient("NamedHttpClient"), then inject IHttpClientFactory and use its CreateClient("NamedHttpClient") method.
            return new HttpClient { BaseAddress = new Uri($"{c.GetRequiredService<NavigationManager>().BaseUri}api/") };
        });
        services.AddRazorPages();
        services.AddPlaygroundServices();
#endif
        services.AddSwaggerGen();

        services.AddCors();
        services.AddControllers();
        //services.AddMvcCore(); 
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
