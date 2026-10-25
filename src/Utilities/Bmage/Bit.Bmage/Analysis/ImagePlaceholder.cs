using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Analysis;

/// <summary>
/// A tiny blurred stand-in for an image, small enough to inline in HTML or store beside a database row.
/// </summary>
/// <remarks>
/// <para>
/// This is the low-quality image placeholder pattern: show something with the right colours and shape immediately, then
/// swap in the real image when it arrives. It is small enough to return in a list endpoint without thinking about it:
/// the default 4x4 grid is 50 bytes, or 67 characters once encoded.
/// </para>
/// <para>
/// Encoded sizes, independent of the source image: 1x1 is 7 characters, 2x2 is 19, 4x4 is 67, 6x6 is 147 and 8x8 is 259.
/// </para>
/// </remarks>
public sealed record ImagePlaceholder
{
    private const int MaxGrid = 8;

    internal ImagePlaceholder(int columns, int rows, Rgba32[] colors, int width, int height)
    {
        Columns = columns;
        Rows = rows;
        Colors = colors;
        SourceWidth = width;
        SourceHeight = height;
    }

    /// <summary>Columns in the colour grid.</summary>
    public int Columns { get; }

    /// <summary>Rows in the colour grid.</summary>
    public int Rows { get; }

    /// <summary>The grid colours, row-major.</summary>
    public IReadOnlyList<Rgba32> Colors { get; }

    /// <summary>Width of the image this was made from.</summary>
    public int SourceWidth { get; }

    /// <summary>Height of the image this was made from.</summary>
    public int SourceHeight { get; }

    /// <summary>The aspect ratio, so a layout can reserve the right space before the image loads.</summary>
    public double AspectRatio => SourceHeight == 0 ? 1 : SourceWidth / (double)SourceHeight;

    /// <summary>The average colour, a reasonable single-colour fallback.</summary>
    public Rgba32 AverageColor
    {
        get
        {
            if (Colors.Count == 0) return Rgba32.Transparent;
            long r = 0, g = 0, b = 0;
            foreach (var c in Colors) { r += c.R; g += c.G; b += c.B; }
            return new Rgba32((byte)(r / Colors.Count), (byte)(g / Colors.Count), (byte)(b / Colors.Count));
        }
    }

    /// <summary>
    /// Builds a placeholder by averaging the image into a small grid. A 4x4 grid is enough to convey composition; 6x6
    /// captures a little more at a cost of a few bytes.
    /// </summary>
    public static ImagePlaceholder Create(ImageBuffer image, int columns = 4, int rows = 4)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (columns is < 1 or > MaxGrid) throw new ArgumentOutOfRangeException(nameof(columns), $"Columns must be between 1 and {MaxGrid}.");
        if (rows is < 1 or > MaxGrid) throw new ArgumentOutOfRangeException(nameof(rows), $"Rows must be between 1 and {MaxGrid}.");

        // Box averaging into exactly the grid, which is what a placeholder is.
        using var small = new ResizeOperation(columns, rows, ResizeMode.Stretch, ResamplingFilter.Box)
            .Apply(image, OperationContext.Default);

        var colors = new Rgba32[columns * rows];
        for (var y = 0; y < rows; y++)
        {
            var row = small.GetRow(y);
            for (var x = 0; x < columns; x++) colors[y * columns + x] = row[x].WithAlpha(255);
        }
        return new ImagePlaceholder(columns, rows, colors, image.Width, image.Height);
    }

    /// <summary>
    /// Encodes to a compact string: grid size, then three bytes per cell, base64url encoded. The default 4x4 grid
    /// produces 67 characters. <see cref="ToString"/> returns the same value, and <see cref="Parse"/> reverses it.
    /// </summary>
    public string ToCompactString()
    {
        var bytes = new byte[2 + Colors.Count * 3];
        bytes[0] = (byte)Columns;
        bytes[1] = (byte)Rows;
        for (var i = 0; i < Colors.Count; i++)
        {
            bytes[2 + i * 3] = Colors[i].R;
            bytes[3 + i * 3] = Colors[i].G;
            bytes[4 + i * 3] = Colors[i].B;
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Parses a string produced by <see cref="ToCompactString"/> or <see cref="ToString"/>. Returns null when the
    /// value is malformed, so a stored placeholder that has been truncated or corrupted degrades to no placeholder
    /// rather than to an exception on a page render.
    /// </summary>
    /// <param name="value">The encoded placeholder.</param>
    /// <param name="sourceWidth">Width of the original image, if known. Only used for <see cref="AspectRatio"/>.</param>
    /// <param name="sourceHeight">Height of the original image, if known.</param>
    public static ImagePlaceholder? Parse(string? value, int sourceWidth = 0, int sourceHeight = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            var bytes = Convert.FromBase64String(padded);
            if (bytes.Length < 5) return null;
            int columns = bytes[0], rows = bytes[1];
            if (columns is < 1 or > MaxGrid || rows is < 1 or > MaxGrid) return null;
            if (bytes.Length < 2 + columns * rows * 3) return null;

            var colors = new Rgba32[columns * rows];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = new Rgba32(bytes[2 + i * 3], bytes[3 + i * 3], bytes[4 + i * 3]);
            return new ImagePlaceholder(columns, rows, colors, sourceWidth, sourceHeight);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// The compact encoded form, identical to <see cref="ToCompactString"/>, so that
    /// <c>ImagePlaceholder.Parse(placeholder.ToString())</c> round-trips. A record's synthesised <c>ToString</c> would
    /// print the member list instead, which <see cref="Parse"/> cannot read back.
    /// </summary>
    public override string ToString() => ToCompactString();

    /// <summary>
    /// Renders the placeholder at any size with smooth interpolation, giving the familiar soft blur without storing an
    /// image. Use it to fill an element before the real image arrives.
    /// </summary>
    public ImageBuffer Render(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
        var output = ImageBuffer.Create(width, height, clear: false);
        try
        {
            for (var y = 0; y < height; y++)
            {
                // Sample at cell centres so the interpolation is smooth rather than blocky at the edges.
                var fy = (y + 0.5f) / height * Rows - 0.5f;
                var y0 = (int)MathF.Floor(fy);
                var ty = fy - y0;
                var row = output.GetRow(y);
                for (var x = 0; x < width; x++)
                {
                    var fx = (x + 0.5f) / width * Columns - 0.5f;
                    var x0 = (int)MathF.Floor(fx);
                    var tx = fx - x0;

                    var c00 = Sample(x0, y0);
                    var c10 = Sample(x0 + 1, y0);
                    var c01 = Sample(x0, y0 + 1);
                    var c11 = Sample(x0 + 1, y0 + 1);
                    var top = Vector3.Lerp(c00, c10, tx);
                    var bottom = Vector3.Lerp(c01, c11, tx);
                    var c = Vector3.Lerp(top, bottom, ty);
                    row[x] = new Rgba32((byte)Math.Clamp(c.X, 0, 255), (byte)Math.Clamp(c.Y, 0, 255), (byte)Math.Clamp(c.Z, 0, 255));
                }
            }
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private Vector3 Sample(int x, int y)
    {
        var c = Colors[Math.Clamp(y, 0, Rows - 1) * Columns + Math.Clamp(x, 0, Columns - 1)];
        return new Vector3(c.R, c.G, c.B);
    }

    /// <summary>
    /// Builds a CSS background value that approximates the placeholder with layered gradients, so a page can show it
    /// with no image request and no canvas at all.
    /// </summary>
    public string ToCssBackground()
    {
        if (Colors.Count == 0) return "background-color:#00000000";
        var layers = new List<string>();
        for (var y = 0; y < Rows; y++)
        {
            for (var x = 0; x < Columns; x++)
            {
                var c = Colors[y * Columns + x];
                var px = (x + 0.5) / Columns * 100;
                var py = (y + 0.5) / Rows * 100;
                var radius = 100.0 / Math.Max(Columns, Rows) * 1.6;
                layers.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"radial-gradient(circle at {px:0.#}% {py:0.#}%, {c.ToHex(false)} 0%, transparent {radius:0.#}%)"));
            }
        }
        return $"background-color:{AverageColor.ToHex(false)};background-image:{string.Join(',', layers)}";
    }
}
