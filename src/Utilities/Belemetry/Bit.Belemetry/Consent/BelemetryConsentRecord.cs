namespace Bit.Belemetry;

/// <summary>A persisted consent decision - the same shape as the browser's <c>bit-belemetry-consent</c> entry.</summary>
internal sealed class BelemetryConsentRecord
{
    /// <summary>The format version.</summary>
    public int V { get; set; } = 1;

    /// <summary>The granted <see cref="BelemetryFeatures"/>.</summary>
    public int Features { get; set; }

    /// <summary>When the decision was made, in Unix milliseconds.</summary>
    public long Time { get; set; }
}
