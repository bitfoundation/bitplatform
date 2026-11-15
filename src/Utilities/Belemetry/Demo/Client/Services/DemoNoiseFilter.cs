using Bit.Belemetry;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// A processor, as an app would write one: it drops the spans and request logs that are the demo talking to itself
/// (its OTLP receiver, the live feed it polls) and, on the server, the requests for static files and
/// the WebAssembly payload - which would otherwise be most of what the server pipeline records.
/// </summary>
/// <remarks>Runs on every .NET-side record before it is queued, so it has to be cheap: a few substring tests.</remarks>
public sealed class DemoNoiseFilter : IBelemetryProcessor
{
    private static readonly string[] _noise =
    [
        .. DemoTelemetry.SelfUrls,
        "/_framework/", "/_content/", "/_blazor", "/images/", "/.well-known/", "/_vs/", "/browserLink",
    ];

    // The site's stylesheets, scripts and icons sit at the root (app.css, site.js, favicon.ico, ...), and
    // MapStaticAssets serves them under fingerprinted names, so they are told apart by their extension.
    private static readonly string[] _staticFiles = [".css", ".js", ".map", ".svg", ".png", ".ico", ".woff2"];

    /// <inheritdoc />
    public bool OnSpan(BelemetrySpan span)
    {
        // Browser spans carry url.full; server request spans carry url.path on .NET 10 and the route everywhere.
        var url = span.Attributes.GetString(BelemetrySemanticConventions.UrlFull)
               ?? span.Attributes.GetString(BelemetrySemanticConventions.UrlPath)
               ?? span.Attributes.GetString(BelemetrySemanticConventions.HttpRoute);

        return url is null || IsNoise(url) is false;
    }

    /// <inheritdoc />
    public bool OnLog(BelemetryLogRecord log)
    {
        // ASP.NET Core's own logs of the same requests ("Request starting HTTP/1.1 GET .../api/telemetry?sessionId=...")
        // carry the request's path in their logging scope. They would be the shared records every reader can list,
        // naming the session id each poll of the live feed carries.
        var path = log.Attributes.GetString("RequestPath") ?? log.Attributes.GetString("Path");
        return path is null || IsNoise(path) is false;
    }

    private static bool IsNoise(string url)
    {
        foreach (var noise in _noise)
        {
            if (url.Contains(noise, StringComparison.OrdinalIgnoreCase)) return true;
        }

        var path = url.AsSpan();
        var end = path.IndexOfAny('?', '#');
        if (end >= 0) path = path[..end];

        foreach (var extension in _staticFiles)
        {
            if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
