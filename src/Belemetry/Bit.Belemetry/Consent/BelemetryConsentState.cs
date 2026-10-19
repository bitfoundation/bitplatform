namespace Bit.Belemetry;

/// <summary>
/// The app-wide consent and user context the .NET pipeline gates on in WebAssembly and Hybrid, where there is one
/// user per process. Fed by <see cref="IBelemetryConsent"/> and by the browser script's context callbacks.
/// </summary>
internal sealed class BelemetryConsentState
{
    private readonly object _lock = new();

    public BelemetryConsentState(BelemetryFeatures initial, bool decided)
    {
        Granted = initial;
        IsDecided = decided;
    }

    /// <summary>The features the user granted (before a decision: the configured default).</summary>
    public BelemetryFeatures Granted { get; private set; }

    /// <summary>Whether the user has made a choice (or one was restored).</summary>
    public bool IsDecided { get; private set; }

    /// <summary>The browser session id, when the script reported one.</summary>
    public string? SessionId { get; private set; }

    /// <summary>The pseudonymous user id, only while <see cref="BelemetryFeatures.UserTracking"/> is granted.</summary>
    public string? PseudoId { get; private set; }

    /// <summary>The authenticated user id, only while <see cref="BelemetryFeatures.UserTracking"/> is granted.</summary>
    public string? UserId { get; private set; }

    /// <summary>Raised after <see cref="Granted"/> or <see cref="IsDecided"/> changed, with the previous grant.</summary>
    public event Action<BelemetryFeatures, BelemetryFeatures>? Changed;

    /// <summary>Whether every flag of <paramref name="feature"/> is granted.</summary>
    public bool IsGranted(BelemetryFeatures feature) => feature != BelemetryFeatures.None && (Granted & feature) == feature;

    /// <summary>Records a decision.</summary>
    public void Decide(BelemetryFeatures granted) => Update(granted, decided: true);

    /// <summary>Updates the grant; raises <see cref="Changed"/> when anything changed.</summary>
    public void Update(BelemetryFeatures granted, bool decided)
    {
        BelemetryFeatures previous;
        bool changed;

        lock (_lock)
        {
            previous = Granted;
            changed = previous != granted || IsDecided != decided;
            Granted = granted;
            IsDecided = decided;

            if ((granted & BelemetryFeatures.UserTracking) == 0)
            {
                PseudoId = null;
                UserId = null;
            }
        }

        if (changed) Changed?.Invoke(previous, granted);
    }

    /// <summary>Updates the user context reported by the browser script or set through <see cref="IBelemetry.SetUserAsync"/>.</summary>
    public void SetContext(string? sessionId, string? pseudoId, string? userId)
    {
        lock (_lock)
        {
            if (sessionId is not null) SessionId = sessionId;
            var tracking = (Granted & BelemetryFeatures.UserTracking) != 0;
            PseudoId = tracking ? pseudoId : null;
            UserId = tracking ? userId : null;
        }
    }

    /// <summary>Sets only the authenticated user id.</summary>
    public void SetUser(string? userId)
    {
        lock (_lock)
        {
            UserId = (Granted & BelemetryFeatures.UserTracking) != 0 ? userId : null;
        }
    }
}
