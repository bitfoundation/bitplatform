using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Gives the whole image one hue and saturation, keeping its lightness: a sepia, cyanotype or monochrome tint whose
/// colour is chosen freely. The colorize option of a hue and saturation dialog.
/// </summary>
public sealed class BmageColorizeAdjustment : BmagePointOperation
{
    /// <param name="hue">The hue in degrees: 0 red, 120 green, 240 blue.</param>
    /// <param name="saturation">The saturation in [0,1].</param>
    /// <param name="lightness">A shift of lightness in [-1,1]: towards black when negative, white when positive.</param>
    /// <param name="amount">How much of the colorised result replaces the original, in [0,1].</param>
    public BmageColorizeAdjustment(float hue, float saturation = 0.25f, float lightness = 0f, float amount = 1f)
    {
        if (!float.IsFinite(hue)) throw new ArgumentOutOfRangeException(nameof(hue), "The hue must be a finite number of degrees.");
        Hue = ((hue % 360) + 360) % 360;
        Saturation = Math.Clamp(float.IsNaN(saturation) ? 0 : saturation, 0f, 1f);
        Lightness = Math.Clamp(float.IsNaN(lightness) ? 0 : lightness, -1f, 1f);
        Amount = Math.Clamp(float.IsNaN(amount) ? 0 : amount, 0f, 1f);
        // With the hue and saturation fixed, the colour is a function of lightness alone, and a piecewise linear one: the
        // lightness shift is linear, and the HSL to RGB conversion is linear either side of half. Linear interpolation
        // in this table is therefore exact at the kink, which falls on an entry, and within a hair of it elsewhere.
        // Alpha zero, so blending towards an entry leaves a pixel's alpha alone.
        _table = new Vector4[TableSize + 1];
        var h = Hue / 60f;
        for (var i = 0; i <= TableSize; i++)
        {
            var l = i / (float)TableSize;
            l += Lightness * (Lightness > 0 ? 1 - l : l);
            _table[i] = new Vector4(BmageColorMath.HslToRgb(h, Saturation, l), 0);
        }
    }

    private const int TableSize = 1024;

    private readonly Vector4[] _table;

    /// <summary>The hue in degrees, normalised to [0,360).</summary>
    public float Hue { get; }

    public float Saturation { get; }

    public float Lightness { get; }

    public float Amount { get; }

    public override string Name => "Colorize";

    public override bool IsIdentity => Amount <= 0;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        // Whole vectors, multiplied by vectors, as the gradient map does, for the interpreter's sake.
        var table = _table;
        var amount = new Vector4(Amount, Amount, Amount, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = Vector4.Clamp(pixels[i], Vector4.Zero, Vector4.One);
            var position = (MathF.Max(p.X, MathF.Max(p.Y, p.Z)) + MathF.Min(p.X, MathF.Min(p.Y, p.Z))) / 2 * TableSize;
            var index = Math.Min(TableSize - 1, (int)position);
            var low = table[index];
            var c = low + (table[index + 1] - low) * new Vector4(position - index);
            pixels[i] = p + (c - p) * amount;
        }
    }
}
