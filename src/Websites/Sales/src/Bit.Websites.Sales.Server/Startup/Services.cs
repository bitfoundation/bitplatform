using System.IO.Compression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.Websites.Sales.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Bit.Belemetry;

namespace Bit.Websites.Sales.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        services.AddHttpClient<TelegramBotApiClient>();
        services.AddScoped<TelegramBotService>();

        services.AddClientSharedServices();

        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddBlazor(configuration);

        services
            .AddControllers();

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

        // Add Azure Application Insights using Belemetry if connection string is configured in appsettings.json
        var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(appInsightsConnectionString) is false)
        {
            services.AddBitBelemetryServices(options =>
            {
                // service.name (cloud_RoleName) comes from the OTEL_SERVICE_NAME environment variable, else the entry assembly name.
                options.Environment = env.EnvironmentName;
                // On an ASP.NET Core host Belemetry only serves its browser script unless its .NET pipeline is turned on.
                options.Server.CollectDotnetTelemetry = true;
                // Blazor's framework files and package assets: a request span each would be static-file noise.
                options.Server.IgnoredRequestPaths.AddRange(["/_framework", "/_content"]);
                options.AddAzureMonitorExporter(appInsightsConnectionString, azure => azure.EnableLiveMetrics = true);
            });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(configuration);
    }
}
