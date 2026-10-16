namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>Where this server's own .NET pipeline sends its telemetry: its own OTLP receiver.</summary>
public static class DemoEndpoints
{
    /// <summary>
    /// The absolute OTLP base URL of this process's receiver, over loopback. <c>Demo:OtlpEndpoint</c> wins when
    /// it is configured; otherwise it is read off the addresses Kestrel is told to listen on (<c>urls</c>,
    /// <c>http_ports</c>) - the same settings the host itself reads, so it cannot point anywhere else.
    /// </summary>
    /// <remarks>
    /// A plain-HTTP address is preferred: the exporter talks to its own process, where TLS adds nothing, and a
    /// development certificate would have to be trusted by the exporter's HttpClient.
    /// </remarks>
    public static string SelfOtlpEndpoint(IConfiguration configuration)
    {
        if (configuration["Demo:OtlpEndpoint"] is { Length: > 0 } configured) return configured;

        var urls = (configuration["urls"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var url = urls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) ?? urls.FirstOrDefault();

        if (url is null && (configuration["http_ports"] ?? configuration["ASPNETCORE_HTTP_PORTS"]) is { Length: > 0 } ports)
        {
            url = $"http://localhost:{ports.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)[0].Trim()}";
        }

        url ??= "http://localhost:5000";

        // A wildcard listen address ("http://+:8080", "http://*:80", "http://0.0.0.0:80", "http://[::]:80") is not
        // something to connect to.
        foreach (var wildcard in new[] { "://+", "://*", "://0.0.0.0", "://[::]" })
        {
            url = url.Replace(wildcard, "://localhost", StringComparison.Ordinal);
        }

        return url.TrimEnd('/') + "/otlp";
    }
}
