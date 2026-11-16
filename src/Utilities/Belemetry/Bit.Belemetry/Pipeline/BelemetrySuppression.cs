using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// Marks the current async flow as Belemetry's own work, so the HTTP calls an exporter makes are neither traced nor
/// logged - otherwise every export would produce a span that is exported by the next export, forever.
/// </summary>
internal static class BelemetrySuppression
{
    private static readonly AsyncLocal<int> _depth = new();

    /// <summary>Whether the current async flow is inside <see cref="Begin"/>.</summary>
    public static bool IsActive => _depth.Value > 0;

    /// <summary>
    /// Suppresses instrumentation until the returned scope is disposed, and takes the app's activity off the flow for as
    /// long: HttpClient's DiagnosticsHandler creates an activity of its own under a current one whatever the listeners
    /// sample, and puts its <c>traceparent</c> on the request - an export an event handler flushed would carry the
    /// user's trace to the collector, and from a WebAssembly page turn into a preflighted request whose header the
    /// collector's CORS policy may refuse.
    /// </summary>
    public static Scope Begin()
    {
        _depth.Value++;
        var current = Activity.Current;
        Activity.Current = null;
        return new Scope(current);
    }

    public readonly struct Scope(Activity? previous) : IDisposable
    {
        public void Dispose()
        {
            if (_depth.Value > 0) _depth.Value--;

            // Back for the rest of a synchronous caller's flow; an activity that stopped meanwhile cannot become current.
            Activity.Current = previous;
        }
    }
}
