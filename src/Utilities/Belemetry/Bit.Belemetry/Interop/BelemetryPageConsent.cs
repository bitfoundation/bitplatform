namespace Bit.Belemetry;

/// <summary>
/// The grant the page last reported over its bridge. A capture checks its browser events against it, live, without
/// holding the bridge itself: a capture an app keeps (a retry queue) must not keep the circuit's scope, its JS runtime
/// and its components alive after the circuit ended. Once the page is gone, its last grant stays the answer.
/// </summary>
internal sealed class BelemetryPageConsent
{
    private int _granted = -1;

    /// <summary>The page's last grant; null until the page reported one.</summary>
    public BelemetryFeatures? Granted
    {
        get
        {
            var granted = Volatile.Read(ref _granted);
            return granted < 0 ? null : (BelemetryFeatures)granted;
        }
    }

    /// <summary>Records the grant the page reported (untrusted on a server: only the defined bits are kept).</summary>
    public void Update(int consent) => Volatile.Write(ref _granted, consent & (int)BelemetryFeatures.All);

    /// <summary>Whether the page grants <paramref name="feature"/> now; true while it reported nothing yet.</summary>
    public bool Allows(BelemetryFeatures feature) => Granted is not { } granted || (granted & feature) == feature;
}
