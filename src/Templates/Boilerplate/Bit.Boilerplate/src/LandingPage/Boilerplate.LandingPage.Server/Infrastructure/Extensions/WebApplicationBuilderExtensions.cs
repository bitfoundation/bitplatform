using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        // [mirror] OpenTelemetry setup - keep in sync with:
        // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/WebApplicationBuilderExtensions.cs (ConfigureOpenTelemetry)
        public void AddLandingOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });

            var telemetry = builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Experimental.Microsoft.Extensions.AI"))
                .WithTracing(tracing => tracing
                    .AddSource("Experimental.Microsoft.Extensions.AI")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        string[] ignored = ["/health", "/alive", "/_content", "/_framework"];

                        options.Filter = context => ignored.Any(segment => context.Request.Path.StartsWithSegments(segment, StringComparison.OrdinalIgnoreCase)) is false;
                    })
                    .AddHttpClientInstrumentation())
                .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName));

            // What the Aspire dashboard reads
            if (string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false)
            {
                telemetry.UseOtlpExporter();
            }

            // Application Insights' own OTLP endpoints need an Entra sign-in, so the server goes through its SDK, with the connection string the browser uses
            if (builder.Configuration["Belemetry:AzureMonitorConnectionString"] is { Length: > 0 } connectionString)
            {
                telemetry.UseAzureMonitorExporter(options => options.ConnectionString = connectionString);
            }
        }
    }
}
