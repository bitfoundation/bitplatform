namespace Bit.Bmage;

/// <summary>Settings for <see cref="BmageSmartCrop"/>.</summary>
public sealed record BmageSmartCropOptions
{
    public static BmageSmartCropOptions Default { get; } = new();

    /// <summary>
    /// Resolution the analysis runs at. Saliency does not need detail, and working small makes this fast enough to run
    /// on every upload. 100 is ample.
    /// </summary>
    public int AnalysisSize { get; init; } = 100;

    /// <summary>Weight given to local detail, which is where the subject usually is.</summary>
    public double DetailWeight { get; init; } = 1.0;

    /// <summary>Weight given to saturated colour, which draws the eye and is rarely background.</summary>
    public double SaturationWeight { get; init; } = 0.3;

    /// <summary>Weight given to skin-like colour, which keeps faces in frame without running face detection.</summary>
    public double SkinWeight { get; init; } = 1.8;

    /// <summary>
    /// How strongly to prefer a crop whose interesting content sits near a rule-of-thirds intersection rather than dead
    /// centre. Set to 0 to ignore composition.
    /// </summary>
    public double CompositionWeight { get; init; } = 0.25;

    /// <summary>How strongly to penalise cutting through a busy area at the crop border.</summary>
    public double EdgePenalty { get; init; } = 0.4;

    /// <summary>Number of candidate positions searched along each axis.</summary>
    public int SearchSteps { get; init; } = 24;
}
