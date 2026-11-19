using System.Numerics;

namespace Bit.Bmage;

/// <summary>Lifts or darkens shadows and recovers or boosts highlights based on pixel luminance. Both amounts in [-1,1].</summary>
public sealed class BmageShadowsHighlightsAdjustment : BmagePointOperation
{
    public BmageShadowsHighlightsAdjustment(float shadows, float highlights)
    {
        Shadows = Math.Clamp(shadows, -1f, 1f);
        Highlights = Math.Clamp(highlights, -1f, 1f);
    }

    public float Shadows { get; }
    public float Highlights { get; }
    public override string Name => "Shadows/Highlights";
    public override bool IsIdentity => Shadows == 0f && Highlights == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0f);
        var s = Shadows * 0.5f;
        var h = Highlights * 0.5f;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var l = Math.Clamp(Vector4.Dot(p, weights), 0f, 1f);
            var ws = (1f - l) * (1f - l);
            var wh = l * l;
            var delta = s * ws + h * wh;
            pixels[i] = new Vector4(p.X + delta, p.Y + delta, p.Z + delta, p.W);
        }
    }
}
