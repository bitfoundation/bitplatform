using Bit.Belemetry;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// A processor, as an app would write one: it drops the spans and request logs that are the demo talking to itself
/// (its OTLP receiver, the live feed it polls) and, on the server, the requests for static files and
/// the WebAssembly payload - which would otherwise be most of what the server pipeline records. Warnings and errors
/// are never dropped. It is noise reduction, not privacy: the session id each poll carries is redacted by
/// <c>RedactQueryParameters</c> (<see cref="DemoTelemetry.ConfigureShared"/>) on whatever record still names it.
/// </summary>
/// <remarks>
/// Runs on every .NET-side record before it is queued, so it has to be cheap: a few substring tests. Kept in step with
/// the copy the Processors page shows (<c>NoiseFilterCode</c> in <c>Pages/ProcessorsPage.razor</c>).
/// </remarks>
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
        // A warning or an error is kept whatever request it was logged in: a failure of the receiver or of the live feed
        // is the demo's own, and the one thing worth seeing about them.
        if (log.Severity >= BelemetrySeverity.Warn) return true;

        // The framework's own logs of the same requests, read only from the attributes their category gives them - an app
        // log can carry a Path of its own ("Loaded {Path}") that names no request:
        // - ASP.NET Core's ("Request starting HTTP/1.1 GET .../api/telemetry?..."): the request path in the logging scope
        //   (RequestPath) and in the Path of the hosting logs themselves.
        // - HttpClient's ("Sending HTTP request GET .../api/telemetry?..."), on the client as on the server: the URL as Uri.
        var category = log.Scope.Name;
        var url = log.Attributes.GetString("RequestPath")
                  ?? (category.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ? log.Attributes.GetString("Path") : null)
                  ?? (category.StartsWith("System.Net.Http.HttpClient", StringComparison.Ordinal) ? log.Attributes.GetString("Uri") : null);

        return url is null || IsNoise(url) is false;
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
