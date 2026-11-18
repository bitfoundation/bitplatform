using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Maps every pixel's luminance to a colour along a gradient: shadows take the first stop's colour, highlights the
/// last's. Duotones, split toning and stylised colour grades.
/// </summary>
/// <remarks>
/// The gradient is interpolated in the stored sRGB values, between stops ordered by offset. The stops' alpha is ignored:
/// the result keeps each pixel's own alpha, and <see cref="Amount"/> mixes it with the original.
/// </remarks>
/// <example>
/// <code>
/// // A navy and gold duotone.
/// pipeline.Apply(BmageGradientMapOperation.Duotone(new BmageRgba32(20, 30, 90), new BmageRgba32(250, 210, 120)));
/// </code>
/// </example>
public sealed class BmageGradientMapOperation : BmagePointOperation
{
    private readonly (float Offset, BmageRgba32 Color)[] _stops;
    // The gradient at 256 positions, alpha zero so blending towards it leaves a pixel's alpha alone.
    private readonly Vector4[] _colors;

    /// <param name="stops">At least two stops; offsets are clamped to [0,1].</param>
    /// <param name="amount">How much of the mapped colour replaces the original, in [0,1].</param>
    public BmageGradientMapOperation(IReadOnlyList<(float Offset, BmageRgba32 Color)> stops, float amount = 1f)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient map needs at least two stops.", nameof(stops));
        if (stops.Any(s => !float.IsFinite(s.Offset))) throw new ArgumentException("Stop offsets must be finite numbers.", nameof(stops));
        _stops = [.. stops.Select(s => (Math.Clamp(s.Offset, 0f, 1f), s.Color)).OrderBy(s => s.Item1)];
        Amount = Math.Clamp(float.IsNaN(amount) ? 0 : amount, 0f, 1f);
        _colors = [.. BuildTable(_stops).Select(color => new Vector4(color, 0))];
    }

    /// <summary>A two-colour gradient map: <paramref name="shadows"/> in the darks, <paramref name="highlights"/> in the lights.</summary>
    public static BmageGradientMapOperation Duotone(BmageRgba32 shadows, BmageRgba32 highlights, float amount = 1f)
        => new([(0f, shadows), (1f, highlights)], amount);

    /// <summary>The stops, ordered by offset.</summary>
    public IReadOnlyList<(float Offset, BmageRgba32 Color)> Stops => _stops;

    public float Amount { get; }

    public override string Name => "Gradient map";

    public override bool IsIdentity => Amount <= 0;

    private static Vector3[] BuildTable((float Offset, BmageRgba32 Color)[] stops)
    {
        var table = new Vector3[256];
        for (var i = 0; i < 256; i++)
        {
            var t = i / 255f;
            var next = 0;
            while (next < stops.Length && stops[next].Offset < t) next++;
            Vector3 color;
            if (next == 0) color = ToVector(stops[0].Color);
            else if (next == stops.Length) color = ToVector(stops[^1].Color);
            else
            {
                var (o0, c0) = stops[next - 1];
                var (o1, c1) = stops[next];
                var f = o1 > o0 ? (t - o0) / (o1 - o0) : 1f;
                color = Vector3.Lerp(ToVector(c0), ToVector(c1), f);
            }
            table[i] = color;
        }
        return table;

        static Vector3 ToVector(BmageRgba32 c) => new Vector3(c.R, c.G, c.B) / 255f;
    }

    public override void ProcessRow(Span<Vector4> pixels)
    {
        // Whole vectors, multiplied by vectors: on the WebAssembly interpreter building a Vector3 from a pixel, or scaling
        // a vector by one float, costs far more than the arithmetic. Alpha's weight and amount are zero, so it is kept.
        var table = _colors;
        var amount = new Vector4(Amount, Amount, Amount, 0);
        var weights = new Vector4(BmageColorMath.Rec709, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var position = Math.Clamp(Vector4.Dot(p, weights), 0f, 1f) * 255f;
            var index = Math.Min(254, (int)position);
            var low = table[index];
            var mapped = low + (table[index + 1] - low) * new Vector4(position - index);
            pixels[i] = p + (mapped - p) * amount;
        }
    }
}
