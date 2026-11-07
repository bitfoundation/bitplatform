namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        // The OTLP endpoint the OTEL_ variables name, when a browser can post to it: over HTTP only, as a browser can't speak gRPC, and with no credentials in its address
        public (Uri Endpoint, string Protocol)? GetBrowserOtlpEndpoint()
        {
            var protocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.Trim().ToLowerInvariant();

            if (protocol is not ("http/protobuf" or "http/json"))
                return null;

            if (Uri.TryCreate(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"], UriKind.Absolute, out var endpoint) is false || endpoint.UserInfo.Length > 0)
                return null;

            return (endpoint, protocol);
        }
    }
}
