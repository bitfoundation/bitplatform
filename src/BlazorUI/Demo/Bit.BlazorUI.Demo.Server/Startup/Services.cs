using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using Bit.BlazorUI.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.ResponseCompression;

namespace Bit.BlazorUI.Demo.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        services.AddExceptionHandler<ServerExceptionHandler>();

        services.AddBlazor(configuration);

        services.AddClientSharedServices();

        
        services.AddCors();

        services
            .AddControllers()
            .AddOData(options => options.EnableQueryFeatures())
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    throw new ResourceValidationException(context.ModelState.Select(ms => (ms.Key, ms.Value!.Errors.Select(e => new LocalizedString(e.ErrorMessage, e.ErrorMessage)).ToArray())).ToArray());
                };
            });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.ForwardedHostHeaderName = "X-Host";
        });

        services.AddResponseCaching();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        services.AddTransient(sp => sp.GetRequiredService<IOptionsSnapshot<AppSettings>>().Value);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(env, configuration);
    }
}
