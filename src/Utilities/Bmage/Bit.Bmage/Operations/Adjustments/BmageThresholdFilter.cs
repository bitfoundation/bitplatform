using System.Numerics;

namespace Bit.Bmage;

/// <summary>Converts to pure black and white by comparing luminance with <c>Level</c> (0..1).</summary>
public sealed class BmageThresholdFilter : BmagePointOperation
{
    public BmageThresholdFilter(float level = 0.5f) => Level = Math.Clamp(level, 0f, 1f);
    public float Level { get; }
    public override string Name => "Threshold";

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0f);
        var level = Level;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var l = Vector4.Dot(p, weights);
            var v = l >= level ? 1f : 0f;
            pixels[i] = new Vector4(v, v, v, p.W);
        }
    }
}
