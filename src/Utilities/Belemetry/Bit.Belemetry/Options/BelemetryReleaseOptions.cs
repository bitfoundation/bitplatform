namespace Bit.Belemetry;

/// <summary>
/// Release markers of a diagnostic capture, on top of <see cref="BelemetryOptions.ServiceVersion"/> and
/// <see cref="BelemetryOptions.Environment"/>. Only what is set here is collected: never the configuration itself.
/// </summary>
public sealed class BelemetryReleaseOptions
{
    /// <summary>The source commit the app was built from (e.g. from a CI variable). Not collected unless set.</summary>
    public string? Commit { get; set; }

    /// <summary>A build or deployment id.</summary>
    public string? BuildId { get; set; }

    /// <summary>The version of the app's configuration or feature-flag set, when it versions them.</summary>
    public string? ConfigurationVersion { get; set; }

    /// <summary>
    /// Extra release metadata (feature flags, a ring, a region): explicitly listed, short, non-secret values only.
    /// Values pass through the privacy rules like any attribute.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = [];
}
