namespace Bit.Belemetry;

/// <summary>How consent is obtained and kept.</summary>
public sealed class BelemetryConsentOptions
{
    /// <summary>
    /// The features granted before the user has decided. Defaults to <see cref="BelemetryFeatures.None"/>
    /// (opt-in: nothing is sent until consent is given). Set it to e.g. <see cref="BelemetryFeatures.Errors"/>
    /// when error reporting is covered by legitimate interest, or to <see cref="BelemetryFeatures.All"/> for apps
    /// that do not need consent at all.
    /// </summary>
    public BelemetryFeatures Default { get; set; } = BelemetryFeatures.None;

    /// <summary>
    /// Whether the user's choice is remembered on the device (<c>localStorage</c> key <c>bit-belemetry-consent</c>,
    /// which records the choice itself and is therefore strictly necessary). Defaults to <see langword="true"/>.
    /// </summary>
    public bool Persist { get; set; } = true;

    /// <summary>
    /// Whether a browser signalling Global Privacy Control (<c>navigator.globalPrivacyControl</c>) never gets the
    /// <see cref="BelemetryFeatures.Analytics"/> features, whatever is requested. Defaults to <see langword="true"/>.
    /// </summary>
    public bool HonorGlobalPrivacyControl { get; set; } = true;

    /// <summary>
    /// How many records of not-yet-granted features are held in memory while the user has not decided, to be sent
    /// if granted and dropped if not. Defaults to 500.
    /// </summary>
    public int PendingLimit { get; set; } = 500;
}
