using Bit.Belemetry;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// A processor, as an app would write one: it drops the spans that are the demo talking to itself
/// (its OTLP receiver, the live feed it polls) and, on the server, the requests for static files and
/// the WebAssembly payload - which would otherwise be most of what the server pipeline records.
/// </summary>
/// <remarks>Runs on every .NET-side record before it is queued, so it has to be cheap: a few substring tests.</remarks>
public sealed class DemoNoiseFilter : IBelemetryProcessor
{
    private static readonly string[] _noise =
    [
        .. DemoTelemetry.SelfUrls,
        "/_framework/", "/_content/", "/_blazor", "/css/", "/js/", "/images/", "/favicon", "/icon-",
        "/.well-known/", "/_vs/", "/browserLink",
    ];

    /// <inheritdoc />
    public bool OnSpan(BelemetrySpan span)
    {
        // Browser spans carry url.full; server request spans carry url.path on .NET 10 and the route everywhere.
        var url = span.Attributes.GetString(BelemetrySemanticConventions.UrlFull)
               ?? span.Attributes.GetString(BelemetrySemanticConventions.UrlPath)
               ?? span.Attributes.GetString(BelemetrySemanticConventions.HttpRoute);

        if (url is null) return true;

        foreach (var noise in _noise)
        {
            if (url.Contains(noise, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }
}
