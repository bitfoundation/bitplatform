
namespace Bit.Bink;

/// <summary>Paint used to fill a path.</summary>
/// <param name="Color">Fill color (its alpha is multiplied with <paramref name="Opacity"/>).</param>
/// <param name="Opacity">Additional opacity 0..1 applied to the whole path as one unit, so self-overlaps do not darken.</param>
/// <param name="Blend">Blend mode.</param>
/// <param name="Rule">Fill rule for self-intersecting paths.</param>
public readonly record struct BinkFillPaint(BinkColor Color, float Opacity = 1f, BinkBlendMode Blend = BinkBlendMode.Normal, BinkFillRule Rule = BinkFillRule.NonZero)
{
    /// <summary>Effective alpha (color alpha times opacity).</summary>
    public float EffectiveAlpha => Color.Alpha * Math.Clamp(Opacity, 0f, 1f);
}
