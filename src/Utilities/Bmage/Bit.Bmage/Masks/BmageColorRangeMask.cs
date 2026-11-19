namespace Bit.Bmage;

/// <summary>
/// Selects pixels close in colour to <see cref="Color"/>: fully covered within <see cref="Tolerance"/>, fading over
/// <see cref="Softness"/>. Distances use the "redmean" weighting, which tracks perceived difference far better than
/// plain RGB distance at almost no cost; 1 is the distance between black and white.
/// </summary>
/// <param name="Color">The colour to select.</param>
public sealed record BmageColorRangeMask(BmageRgba32 Color) : BmageMask
{
    /// <summary>The largest distance that is still fully covered. Default 0.15.</summary>
    public float Tolerance
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.15f;

    /// <summary>How much further the coverage takes to fade to nothing. Default 0.1.</summary>
    public float Softness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.1f;

    public override string Kind => "Colour range";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        var source = context.RequireSource("colour range");
        var color = Color;
        var tolerance = Tolerance;
        var softness = Softness;
        var window = context.Window;
        BmageParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width * 2L, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var pixels = source.GetRow(y).Slice(window.Left, window.Width);
                var row = target.GetRow(y - window.Top);
                for (var x = 0; x < row.Length; x++)
                {
                    var d = Distance(pixels[x], color);
                    row[x] = d <= tolerance ? (byte)255 : softness <= 0 ? (byte)0 : BmageRgba32.ClampToByte(Math.Max(0, 1 - (d - tolerance) / softness) * 255f);
                }
            }
        });
    }

    /// <summary>The redmean distance between two colours, scaled so black to white is 1.</summary>
    internal static float Distance(BmageRgba32 a, BmageRgba32 b)
    {
        const float inv = 1f / 255f;
        var mean = (a.R + b.R) * 0.5f * inv;
        var dr = (a.R - b.R) * inv;
        var dg = (a.G - b.G) * inv;
        var db = (a.B - b.B) * inv;
        return MathF.Sqrt((2 + mean) * dr * dr + 4 * dg * dg + (3 - mean) * db * db) / 3f;
    }
}
