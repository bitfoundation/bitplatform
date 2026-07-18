namespace Bit.Bmotion;

/// <summary>
/// The color space used to interpolate between two colors. Set per-transition
/// (<c>Bm.Tween(colorSpace: BmColorSpace.Oklab)</c>) or globally via <c>&lt;BmotionConfig&gt;</c>.
/// </summary>
public enum BmColorSpace
{
    /// <summary>
    /// Per-channel sRGB interpolation (default, matching Framer Motion). Fast and predictable, but
    /// complementary colors can pass through a desaturated grey mid-point.
    /// </summary>
    Srgb = 0,

    /// <summary>
    /// Perceptual OKLab interpolation. Keeps mid-points saturated (e.g. blue→yellow stays vivid) at
    /// a small extra cost per frame.
    /// </summary>
    Oklab = 1,
}
