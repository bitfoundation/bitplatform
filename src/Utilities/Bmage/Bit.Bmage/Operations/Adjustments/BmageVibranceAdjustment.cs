using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Increases saturation of muted colours more than already saturated ones, avoiding clipping of skin tones.
/// <c>Amount</c> in [-1,1].
/// </summary>
public sealed class BmageVibranceAdjustment : BmagePointOperation
{
    public BmageVibranceAdjustment(float amount) => Amount = Math.Clamp(amount, -1f, 1f);
    public float Amount { get; }
    public override string Name => "Vibrance";
    public override bool IsIdentity => Amount == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var amount = Amount;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var max = MathF.Max(p.X, MathF.Max(p.Y, p.Z));
            var avg = (p.X + p.Y + p.Z) / 3f;
            // Move channels away from (positive) or towards (negative) the max channel, scaled by how unsaturated the pixel is.
            var saturation = Math.Clamp((max - avg) * 1.5f, 0f, 1f);
            var k = (1f - saturation) * amount * 0.6f;
            p.X += (p.X - max) * k;
            p.Y += (p.Y - max) * k;
            p.Z += (p.Z - max) * k;
            pixels[i] = p;
        }
    }
}
