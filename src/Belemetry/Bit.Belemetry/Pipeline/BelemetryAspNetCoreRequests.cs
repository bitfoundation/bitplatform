using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.Belemetry;

/// <summary>
/// Gives ASP.NET Core's request <see cref="Activity"/> the HTTP semantic conventions (<c>http.request.method</c>,
/// <c>url.scheme</c>, <c>http.route</c>, <c>http.response.status_code</c>, <c>error.type</c>, ...) that the OpenTelemetry
/// ASP.NET Core instrumentation would otherwise add, and that ASP.NET Core itself leaves off on .NET 8 and 9.
/// </summary>
/// <remarks>
/// Belemetry cannot reference ASP.NET Core (it also runs in the browser), and reading the <c>HttpContext</c> of the
/// hosting <c>DiagnosticListener</c> events would need reflection, which is not trimming safe. The hosting metric
/// <c>http.server.request.duration</c> carries exactly these values as tags, and ASP.NET Core records it on every
/// supported version while the request Activity is still current and not yet stopped, so a
/// <see cref="MeterListener"/> copies them onto that Activity just before it stops.
/// </remarks>
internal sealed class BelemetryAspNetCoreRequests : IDisposable
{
    internal const string ActivityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
    internal const string MeterName = "Microsoft.AspNetCore.Hosting";
    internal const string DurationInstrument = "http.server.request.duration";

    /// <summary>
    /// The .NET 10 switch behind which ASP.NET Core sets <c>url.path</c>, <c>server.address</c>, <c>server.port</c>
    /// and friends on the request Activity itself (it is suppressed unless turned off). Ignored by .NET 8 and 9.
    /// </summary>
    internal const string SuppressSwitch = "Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData";

    private readonly MeterListener _listener;

    public BelemetryAspNetCoreRequests()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = static (instrument, listener) =>
            {
                if (instrument.Name == DurationInstrument && instrument.Meter.Name == MeterName) listener.EnableMeasurementEvents(instrument);
            },
        };
        _listener.SetMeasurementEventCallback<double>(static (_, _, tags, _) => Enrich(Activity.Current, tags));
        _listener.Start();
    }

    /// <summary>
    /// Lets ASP.NET Core 10 tag its request Activity natively, unless the app decided either way itself. It is read
    /// once when the web host starts, which is after the logger factory (and so the pipeline) was built.
    /// </summary>
    public static void EnableNativeTags()
    {
        if (AppContext.TryGetSwitch(SuppressSwitch, out _) is false) AppContext.SetSwitch(SuppressSwitch, false);
    }

    /// <summary>Copies the request metric's tags onto the request Activity, never overwriting a tag already set.</summary>
    internal static void Enrich(Activity? activity, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (activity is null || activity.IsStopped || activity.Recorded is false || activity.OperationName != ActivityName) return;

        foreach (var (key, value) in tags)
        {
            if (value is not null && activity.GetTagItem(key) is null) activity.SetTag(key, value);
        }
    }

    /// <summary>
    /// The span name the HTTP server conventions define: <c>{method} {http.route}</c>, or just <c>{method}</c> without a
    /// route (a raw path would make the name high-cardinality). Null when the method is unknown.
    /// </summary>
    internal static string? GetSpanName(string? method, string? route)
    {
        if (string.IsNullOrEmpty(method)) return null;
        if (method == "_OTHER") method = "HTTP";
        return string.IsNullOrEmpty(route) ? method : $"{method} {route}";
    }

    public void Dispose() => _listener.Dispose();
}
