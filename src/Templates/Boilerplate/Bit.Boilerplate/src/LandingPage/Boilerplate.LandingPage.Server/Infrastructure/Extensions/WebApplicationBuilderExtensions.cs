using Bit.Belemetry;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        // [mirror] telemetry setup - keep in sync with:
        // - src/Server/Boilerplate.Server.Shared/Infrastructure/Extensions/WebApplicationBuilderExtensions.cs (ConfigureTelemetry)
        public void AddLandingTelemetry()
        {
            builder.Services.AddBitBelemetryServices(options =>
            {
                options.Server.CollectDotnetTelemetry = true;
                options.Server.IgnoredRequestPaths.AddRange(["/health", "/alive", "/_content", "/_framework"]);

                options.Traces.Sources.Add("Experimental.Microsoft.Extensions.AI");
                options.Metrics.Meters.Add("Experimental.Microsoft.Extensions.AI");
            });
        }
    }
}
