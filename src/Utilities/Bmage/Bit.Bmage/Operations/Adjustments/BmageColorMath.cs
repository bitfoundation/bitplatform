using System.Numerics;

namespace Bit.Bmage;

/// <summary>Colour conversions and curves shared by the colour adjustments.</summary>
internal static class BmageColorMath
{
    /// <summary>The Rec. 709 luma weights, as the library's other luminance measures use.</summary>
    public static readonly Vector3 Rec709 = new(0.2126f, 0.7152f, 0.0722f);

    /// <summary>A smooth step from 0 at <paramref name="edge0"/> to 1 at <paramref name="edge1"/>.</summary>
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>The colour of a hue in sextants [0,6), a saturation and a lightness.</summary>
    public static Vector3 HslToRgb(float h, float s, float l)
    {
        l = Math.Clamp(l, 0f, 1f);
        if (s <= 0) return new Vector3(l);
        var q = l < 0.5f ? l * (1f + s) : l + s - l * s;
        var p = 2f * l - q;
        return new Vector3(Channel(p, q, h + 2f), Channel(p, q, h), Channel(p, q, h - 2f));

        static float Channel(float p, float q, float t)
        {
            if (t < 0) t += 6f;
            if (t >= 6f) t -= 6f;
            if (t < 1f) return p + (q - p) * t;
            if (t < 3f) return q;
            if (t < 4f) return p + (q - p) * (4f - t);
            return p;
        }
    }
}
