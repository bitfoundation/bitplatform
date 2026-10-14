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

    /// <summary>Suppresses instrumentation until the returned scope is disposed.</summary>
    public static Scope Begin()
    {
        _depth.Value++;
        return default;
    }

    public readonly struct Scope : IDisposable
    {
        public void Dispose()
        {
            if (_depth.Value > 0) _depth.Value--;
        }
    }
}
