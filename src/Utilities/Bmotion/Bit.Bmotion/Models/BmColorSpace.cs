namespace Bit.Bmotion;

/// <summary>
/// The color space used to interpolate between two colors. Set per-transition
/// (<c>Bm.Tween(colorSpace: BmColorSpace.Oklab)</c>) or globally via <c>&lt;BmotionConfig&gt;</c>.
/// </summary>
public enum BmColorSpace
{
    /// <summary>
    /// Per-channel sRGB interpolation (default, matching Framer Motion). Fast and predictable, but
    /// between two distant colors it dips darker and muddier in the middle than its two ends suggest.
    /// </summary>
    Srgb = 0,

    /// <summary>
    /// Perceptual OKLab interpolation: the lightness changes evenly between the two ends, so mid-points
    /// stay clean and bright (blue→yellow passes a light blue-grey rather than sRGB's dark one), at a
    /// small extra cost per frame.
    /// </summary>
    Oklab = 1,
}
