namespace Bit.Bmage;

/// <summary>
/// Selects pixels by brightness: fully covered where the luminance (Rec. 709, in [0,1]) lies between
/// <see cref="Low"/> and <see cref="High"/>, fading over <see cref="Softness"/> beyond them. Recover highlights
/// with <c>new BmageLuminanceRangeMask(0.75f, 1f)</c>, lift shadows with <c>new BmageLuminanceRangeMask(0f, 0.25f)</c>.
/// </summary>
/// <param name="Low">The darkest fully covered luminance.</param>
/// <param name="High">The brightest fully covered luminance.</param>
public sealed record BmageLuminanceRangeMask(float Low, float High) : BmageMask
{
    public float Low { get; init; } = Math.Clamp(Low, 0f, 1f);
    public float High { get; init; } = High >= Low ? Math.Clamp(High, 0f, 1f) : throw new ArgumentOutOfRangeException(nameof(High), "The range's top must not be below its bottom.");

    /// <summary>How far beyond the range, in luminance, the coverage takes to fade to nothing. Default 0.1.</summary>
    public float Softness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.1f;

    public override string Kind => "Luminance range";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        var source = context.RequireSource("luminance range");
        // Luminance is a byte, so coverage is a 256 entry table.
        var table = new byte[256];
        for (var v = 0; v < 256; v++)
        {
            var l = v / 255f;
            var distance = l < Low ? Low - l : l > High ? l - High : 0f;
            table[v] = distance <= 0 ? (byte)255 : Softness <= 0 ? (byte)0 : BmageRgba32.ClampToByte(Math.Max(0, 1 - distance / Softness) * 255f);
        }
        var window = context.Window;
        BmageParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width / 2, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var pixels = source.GetRow(y).Slice(window.Left, window.Width);
                var row = target.GetRow(y - window.Top);
                for (var x = 0; x < row.Length; x++) row[x] = table[BmageMaskBuffer.Luma(pixels[x])];
            }
        });
    }
}
