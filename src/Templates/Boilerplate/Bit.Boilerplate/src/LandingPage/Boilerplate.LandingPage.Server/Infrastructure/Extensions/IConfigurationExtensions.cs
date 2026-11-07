namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        // Belemetry:OtlpEndpoint, else the OTLP endpoint the OTEL_ variables name, when a browser can post to it: over HTTP or HTTPS only, as a browser can't speak gRPC, and with no credentials in its address
        public (Uri Endpoint, string Protocol)? GetBrowserOtlpEndpoint()
        {
            if (string.IsNullOrWhiteSpace(configuration["Belemetry:OtlpEndpoint"]) is false)
            {
                return Uri.TryCreate(configuration["Belemetry:OtlpEndpoint"], UriKind.Absolute, out var browserEndpoint) && browserEndpoint.Scheme is "http" or "https" && browserEndpoint.UserInfo.Length is 0
                    ? (browserEndpoint, "http/protobuf")
                    : null;
            }

            var protocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.Trim().ToLowerInvariant();

            if (protocol is not ("http/protobuf" or "http/json"))
                return null;

            if (Uri.TryCreate(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"], UriKind.Absolute, out var endpoint) is false || endpoint.Scheme is not ("http" or "https") || endpoint.UserInfo.Length > 0)
                return null;

            return (endpoint, protocol);
        }
    }
}
