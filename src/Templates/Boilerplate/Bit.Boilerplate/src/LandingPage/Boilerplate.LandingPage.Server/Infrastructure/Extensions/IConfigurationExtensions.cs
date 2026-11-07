namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        // The OTLP endpoint the OTEL_ variables name, when a browser can post to it: never over gRPC, which a browser can't speak
        public Uri? GetBrowserOtlpEndpoint()
        {
            if (configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] is "grpc")
                return null;

            return Uri.TryCreate(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"], UriKind.Absolute, out var endpoint) ? endpoint : null;
        }
    }
}
