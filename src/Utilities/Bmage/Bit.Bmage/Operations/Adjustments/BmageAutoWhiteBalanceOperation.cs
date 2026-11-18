using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Removes a colour cast by measuring the image: the average of its mid-tones, which in most photos is close to grey,
/// is made grey. The automatic counterpart of <see cref="BmageWhiteBalanceAdjustment"/>.
/// </summary>
/// <remarks>
/// <para>
/// The "grey world" assumption holds for most scenes and fails for some: a sunset or a forest is meant to be dominated
/// by one colour, which is why <see cref="Strength"/> defaults to a partial correction. Clipped highlights and deep
/// shadows are left out of the average, as their colour says little about the light.
/// </para>
/// <para>
/// The gains come from the pixels the operation is applied to, so on a proxy preview they come from the proxy. The two
/// agree closely because the average of an image barely changes when it is shrunk.
/// </para>
/// </remarks>
public sealed class BmageAutoWhiteBalanceOperation : BmageImageOperation
{
    /// <param name="strength">How far to correct, in [0,1].</param>
    public BmageAutoWhiteBalanceOperation(float strength = 0.8f) => Strength = Math.Clamp(float.IsNaN(strength) ? 0 : strength, 0f, 1f);

    /// <summary>How far towards fully neutral to correct, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto white balance";

    public override bool IsIdentity => Strength <= 0;

    /// <summary>
    /// The correction the operation would make to <paramref name="image"/>, or null when the image has no mid-tones to
    /// measure.
    /// </summary>
    public BmageWhiteBalanceAdjustment? Measure(BmageImageBuffer image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        double r = 0, g = 0, b = 0;
        long count = 0;
        for (var y = 0; y < image.Height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            foreach (var p in image.GetRow(y))
            {
                if (p.A < 128) continue;
                var max = Math.Max(p.R, Math.Max(p.G, p.B));
                var min = Math.Min(p.R, Math.Min(p.G, p.B));
                // Clipped highlights and near-black shadows carry no reliable colour.
                if (max >= 250 || max < 16) continue;
                // Strongly saturated pixels are the scene's colours, not the light's.
                if (max - min > 160) continue;
                r += p.R;
                g += p.G;
                b += p.B;
                count++;
            }
        }
        if (count == 0) return null;
        var mean = new BmageRgba32((byte)Math.Round(r / count), (byte)Math.Round(g / count), (byte)Math.Round(b / count));
        return BmageWhiteBalanceAdjustment.FromNeutral(mean, Strength);
    }

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        if (IsIdentity || Measure(source, context.CancellationToken) is not { } correction)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        return correction.Apply(source, context);
    }
}
