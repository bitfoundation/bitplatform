using System.IO.Compression;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Bit.Websites.Careers.Server.Services;
using Microsoft.AspNetCore.Antiforgery;
using Bit.Belemetry;

namespace Bit.Websites.Careers.Server.Startup;

public static class Services
{
    public static void Add(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        // Services being registered here can get injected into controllers and services in Server project.

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

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
            // Belemetry skips an exporter whose connection string it cannot parse and says so at Debug level only, which
            // nothing records on the server, so a broken one stops the startup here instead of silently sending nothing.
            var instrumentationKey = appInsightsConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .LastOrDefault(pair => pair.Length is 2 && string.Equals(pair[0], "InstrumentationKey", StringComparison.OrdinalIgnoreCase))?[1];
            if (Guid.TryParse(instrumentationKey, out _) is false)
                throw new InvalidOperationException("ApplicationInsights:ConnectionString has no valid InstrumentationKey.");

            services.AddBitBelemetryServices(options =>
            {
                // service.name (cloud_RoleName) comes from the OTEL_SERVICE_NAME environment variable, else the entry assembly name.
                options.Environment = env.EnvironmentName;
                // On an ASP.NET Core host Belemetry only serves its browser script unless its .NET pipeline is turned on.
                options.Server.CollectDotnetTelemetry = true;
                // Batches Application Insights could not take are kept on disk and sent once it is back, instead of being lost
                // with the outage: in {LocalApplicationData}/bit-belemetry/{service.name}, or Belemetry:Persistence:Directory.
                options.Server.Features |= BelemetryFeatures.Persistence;
                // Blazor's framework files and package assets would be a request span each, and the Blazor circuit one
                // request as long as the connection, which would skew every response time chart.
                options.Server.IgnoredRequestPaths.AddRange(["/_framework", "/_content", "/_blazor"]);
                // Blazor's per-event spans and its and the runtime's meters, which Application Insights never got before.
                // Removed in code, as a list bound from configuration can only add to them.
                options.Traces.Sources.Remove("Microsoft.AspNetCore.Components*");
                options.Metrics.Meters.RemoveAll(meter => meter is "Microsoft.AspNetCore.Components*" or "System.Runtime");

                // appsettings.json is server-owned, so the Belemetry section there tunes all of the above without a redeploy,
                // e.g. Traces:SamplingRatio, which is 1.0 here: the exporter does not scale the counts Application Insights
                // shows by it, so a lower ratio lowers the request and failure counts too.
                configuration.GetSection("Belemetry").Bind(options);

                options.AddAzureMonitorExporter(appInsightsConnectionString, azure => azure.EnableLiveMetrics = true);
            });
        }

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen();

        services.AddHealthChecks(configuration);
    }
}
