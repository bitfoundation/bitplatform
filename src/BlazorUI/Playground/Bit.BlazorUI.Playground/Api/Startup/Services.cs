using System.IO.Compression;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.BlazorUI.Playground.Shared.Dtos;

#if BlazorWebAssembly
using Microsoft.AspNetCore.Components;
#endif

namespace Bit.BlazorUI.Playground.Api.Startup;

public static class Services
{
    public static void Add(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
#if BlazorWebAssembly
        services.AddScoped(c =>
        {
            // this is for pre rendering of blazor client/wasm
            // Using this registration + registrations provided in Program.cs/Startup.cs of Bit.BlazorUI.Playground.Web project,
            // you can inject HttpClient and call Bit.BlazorUI.Playground.Api api controllers in blazor pages.
            // for other usages of http client, for example calling 3rd party apis, please use services.AddHttpClient("NamedHttpClient"), 
            // then inject IHttpClientFactory and use its CreateClient("NamedHttpClient") method.
            return new HttpClient { BaseAddress = new Uri($"{c.GetRequiredService<NavigationManager>().BaseUri}api/") };
        });
        services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader>();
        services.AddRazorPages();
        services.AddPlaygroundServices();
#endif
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<ODataOperationFilter>();
        });

        services.AddCors();

        services.AddControllers()
            .AddOData();
        
        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
        });

        services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
    }
}
