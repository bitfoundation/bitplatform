namespace Bit.Belemetry.Tests.Harness.Web;

/// <summary>The settings the harness host reads, besides <see cref="HarnessRenderModes.ConfigurationKey"/>.</summary>
public static class HarnessConfiguration
{
    /// <summary>
    /// <c>BelemetryHarness:ServerPipeline=true</c> turns <see cref="BelemetryServerOptions.CollectDotnetTelemetry"/> on.
    /// Off by default, like the library's own default.
    /// </summary>
    public const string ServerPipelineKey = "BelemetryHarness:ServerPipeline";

    /// <summary>
    /// <c>BelemetryHarness:CollectorOrigin</c>: the origin the exporters post to. Defaults to the first address in
    /// <c>urls</c> (<c>--urls</c> / <c>ASPNETCORE_URLS</c>), which is what makes the browser's exports same-origin.
    /// </summary>
    public const string CollectorOriginKey = "BelemetryHarness:CollectorOrigin";

    public static string CollectorOrigin(IConfiguration configuration)
    {
        if (configuration[CollectorOriginKey] is { Length: > 0 } explicitOrigin) return explicitOrigin.TrimEnd('/');

        var first = configuration["urls"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (first is null) return "http://localhost";

        // Wildcard bindings are not addresses a browser can post to.
        return first.Replace("://+", "://localhost").Replace("://*", "://localhost").Replace("://0.0.0.0", "://localhost").TrimEnd('/');
    }
}
