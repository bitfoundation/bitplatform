
namespace Bit.Bink;

/// <summary>Paint used to stroke (outline) a path.</summary>
/// <param name="Color">Line color.</param>
/// <param name="Width">Line width in the current coordinate space.</param>
/// <param name="Opacity">Additional opacity 0..1 applied to the whole path as one unit.</param>
/// <param name="Blend">Blend mode.</param>
/// <param name="Cap">Line cap.</param>
/// <param name="Join">Line join.</param>
/// <param name="Dash">Optional dash pattern (on, off, ...) in the current coordinate space.</param>
public readonly record struct BinkStrokePaint(BinkColor Color, float Width, float Opacity = 1f, BinkBlendMode Blend = BinkBlendMode.Normal, BinkLineCap Cap = BinkLineCap.Round, BinkLineJoin Join = BinkLineJoin.Round, float[]? Dash = null)
{
    /// <summary>Effective alpha (color alpha times opacity).</summary>
    public float EffectiveAlpha => Color.Alpha * Math.Clamp(Opacity, 0f, 1f);
}
