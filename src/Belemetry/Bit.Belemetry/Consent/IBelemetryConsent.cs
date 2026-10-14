namespace Bit.Belemetry;

/// <summary>
/// Reads and changes the user's consent at runtime, per <see cref="BelemetryFeatures"/> - the API behind a consent
/// banner or a privacy settings page.
/// </summary>
/// <remarks>
/// On WebAssembly and Hybrid the decision is app-wide and applies to the browser script and the .NET pipeline alike.
/// On Blazor Server it belongs to the circuit's user and governs their browser script; it needs an interactive render
/// mode, since it talks to that user's browser.
/// </remarks>
public interface IBelemetryConsent
{
    /// <summary>The features that can be consented to - the configured <see cref="BelemetryOptions.Features"/>.</summary>
    BelemetryFeatures Available { get; }

    /// <summary>The features currently granted.</summary>
    BelemetryFeatures Granted { get; }

    /// <summary>Whether the user has made a choice (or one was restored from a previous visit).</summary>
    bool IsDecided { get; }

    /// <summary>Whether every flag of <paramref name="feature"/> is granted.</summary>
    bool IsGranted(BelemetryFeatures feature);

    /// <summary>
    /// Records the user's decision: exactly <paramref name="features"/> are granted, everything else is revoked
    /// (queued data of revoked features is discarded, and their device storage cleared).
    /// </summary>
    ValueTask SetAsync(BelemetryFeatures features);

    /// <summary>Grants <paramref name="features"/> in addition to what is granted.</summary>
    ValueTask GrantAsync(BelemetryFeatures features);

    /// <summary>Revokes <paramref name="features"/>, keeping the rest.</summary>
    ValueTask RevokeAsync(BelemetryFeatures features);

    /// <summary>Reloads the state from the browser (e.g. after the first interactive render on Blazor Server).</summary>
    ValueTask RefreshAsync();

    /// <summary>Raised after the granted features or the decided state changed.</summary>
    event EventHandler? Changed;
}
