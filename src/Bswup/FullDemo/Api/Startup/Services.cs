using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Bit.Bswup.Demo.Api.Startup;

public static class Services
{
    public static void Add(IServiceCollection services)
    {
        services.AddRazorPages();
      
        services.AddCors();

        services.AddControllers();
        
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
