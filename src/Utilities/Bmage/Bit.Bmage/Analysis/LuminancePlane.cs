using System.Drawing;

namespace Bit.Bmage.Analysis;

/// <summary>
/// The luminance of an image, or of a region of it, reduced by a whole factor so its longer side is at most a given
/// size: what an analysis that looks at structure rather than colour reads, at a cost that does not grow with the
/// photo's resolution.
/// </summary>
internal sealed class LuminancePlane
{
    private LuminancePlane(byte[] values, int width, int height, int factor)
    {
        Values = values;
        Width = width;
        Height = height;
        Factor = factor;
    }

    /// <summary>Row-major luminance, 0..255, Rec. 709 weights on the encoded values.</summary>
    public byte[] Values { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>How many source pixels across and down each value averages.</summary>
    public int Factor { get; }

    /// <summary>
    /// Averages blocks of <c>factor × factor</c> pixels of <paramref name="area"/>, the factor being the smallest that
    /// brings its longer side to <paramref name="maxSize"/> or less. Transparent pixels count as the colour they carry;
    /// an analysis of a cut-out should composite it first.
    /// </summary>
    public static LuminancePlane Create(ImageBuffer image, Rectangle area, int maxSize, CancellationToken cancellationToken)
    {
        var longer = Math.Max(area.Width, area.Height);
        var factor = Math.Max(1, (longer + maxSize - 1) / maxSize);
        // A side shorter than the factor is averaged whole rather than read beyond.
        int across = Math.Min(factor, area.Width), down = Math.Min(factor, area.Height);
        int width = area.Width / across, height = area.Height / down;
        var values = new byte[width * height];
        // A block of a large image at a small analysis size holds tens of thousands of pixels: its sum outgrows an int.
        var sums = new long[width];
        var count = (long)across * down * 256;
        for (var y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Clear(sums);
            for (var dy = 0; dy < down; dy++)
            {
                var row = image.GetRow(area.Top + y * down + dy).Slice(area.Left, width * across);
                for (var x = 0; x < width; x++)
                {
                    var sum = 0;
                    foreach (var p in row.Slice(x * across, across)) sum += p.R * 54 + p.G * 183 + p.B * 19;
                    sums[x] += sum;
                }
            }
            var line = values.AsSpan(y * width, width);
            for (var x = 0; x < width; x++) line[x] = (byte)((sums[x] + count / 2) / count);
        }
        return new LuminancePlane(values, width, height, factor);
    }
}
