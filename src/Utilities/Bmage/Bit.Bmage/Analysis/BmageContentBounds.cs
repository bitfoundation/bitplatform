using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bit.Bmage;

/// <summary>
/// Finds the part of an image that is not border: the rectangle to crop to when trimming transparent or uniform edges
/// from a cut-out, a scan or a screenshot.
/// </summary>
/// <remarks>
/// The result depends on the pixels, so trimming is not itself an operation: an operation's output size must be known
/// from its input size alone, which is what lets a document report its size, place annotations and estimate memory
/// before rendering. Find the bounds once and record the crop they give, which then replays exactly.
/// </remarks>
/// <example>
/// <code>
/// using var image = await loader.LoadAsync(file);
/// var pipeline = BmageImagePipeline.Create();
/// if (BmageContentBounds.Find(image, new BmageTrimOptions { Padding = 8 }) is { } content)
///     pipeline = pipeline.Crop(content);
/// using var trimmed = pipeline.Resize(1000, 1000, BmageResizeMode.Contain, background: BmageRgba32.White).Execute(image);
/// </code>
/// </example>
public static class BmageContentBounds
{
    /// <summary>
    /// The smallest rectangle holding every pixel that is not background, grown by the padding, or null when the
    /// whole image is background.
    /// </summary>
    public static Rectangle? Find(BmageImageBuffer image, BmageTrimOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= BmageTrimOptions.Default;
        var tolerance = (int)MathF.Round(options.Tolerance * 255);
        var transparent = options.Mode switch
        {
            BmageTrimMode.Transparent => true,
            BmageTrimMode.Color => false,
            _ => HasTransparentCorner(image, tolerance),
        };
        // Transparency is background whatever its colour: every colour channel may differ entirely.
        var background = new Background(transparent ? default : options.Color ?? image.GetRow(0)[0],
            transparent ? new BmageRgba32(255, 255, 255, (byte)tolerance) : new BmageRgba32((byte)tolerance, (byte)tolerance, (byte)tolerance, (byte)tolerance));

        // Rows from the top and the bottom until one holds content, then only the columns outside the content found so
        // far: the cost is the border's pixels, not the image's.
        int width = image.Width, height = image.Height;
        var top = 0;
        while (top < height && background.FirstContent(image.GetRow(top)) < 0)
        {
            if ((top & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            top++;
        }
        if (top == height) return null;
        var bottom = height - 1;
        while (bottom > top && background.FirstContent(image.GetRow(bottom)) < 0)
        {
            if ((bottom & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            bottom--;
        }
        int left = width, right = -1;
        for (var y = top; y <= bottom; y++)
        {
            if (((y - top) & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = image.GetRow(y);
            if (background.FirstContent(row[..left]) is var first and >= 0) left = first;
            if (background.LastContent(row[(right + 1)..]) is var last and >= 0) right += 1 + last;
        }

        var bounds = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        bounds.Inflate(options.Padding, options.Padding);
        bounds.Intersect(image.Bounds);
        return bounds;
    }

    /// <summary>
    /// A background colour and how far each channel may stray from it, tested four pixels at a time where vectors are
    /// accelerated, as they are with WebAssembly's SIMD: the trim then costs about what reading the border does.
    /// </summary>
    private readonly struct Background(BmageRgba32 color, BmageRgba32 tolerance)
    {
        private readonly Vector128<byte> _color = Vector128.Create(color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A);
        private readonly Vector128<byte> _tolerance = Vector128.Create(tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A);

        public bool Matches(BmageRgba32 p)
            => Math.Abs(p.R - color.R) <= tolerance.R && Math.Abs(p.G - color.G) <= tolerance.G
               && Math.Abs(p.B - color.B) <= tolerance.B && Math.Abs(p.A - color.A) <= tolerance.A;

        /// <summary>The index of the first pixel that is not background, or -1.</summary>
        public int FirstContent(ReadOnlySpan<BmageRgba32> row)
        {
            var x = 0;
            if (Vector128.IsHardwareAccelerated)
            {
                var bytes = MemoryMarshal.AsBytes(row);
                for (; x + 4 <= row.Length; x += 4)
                    if (Strays(Vector128.Create(bytes.Slice(x * 4, 16)))) break;
            }
            for (; x < row.Length; x++)
                if (!Matches(row[x])) return x;
            return -1;
        }

        /// <summary>The index of the last pixel that is not background, or -1.</summary>
        public int LastContent(ReadOnlySpan<BmageRgba32> row)
        {
            var x = row.Length;
            if (Vector128.IsHardwareAccelerated)
            {
                var bytes = MemoryMarshal.AsBytes(row);
                for (; x >= 4; x -= 4)
                    if (Strays(Vector128.Create(bytes.Slice((x - 4) * 4, 16)))) break;
            }
            for (x--; x >= 0; x--)
                if (!Matches(row[x])) return x;
            return -1;
        }

        /// <summary>Whether any channel of the four pixels strays further from the background than allowed.</summary>
        private bool Strays(Vector128<byte> pixels)
            => Vector128.GreaterThanAny(Vector128.Max(pixels, _color) - Vector128.Min(pixels, _color), _tolerance);
    }

    private static bool HasTransparentCorner(BmageImageBuffer image, int tolerance)
    {
        var top = image.GetRow(0);
        var bottom = image.GetRow(image.Height - 1);
        return top[0].A <= tolerance || top[^1].A <= tolerance || bottom[0].A <= tolerance || bottom[^1].A <= tolerance;
    }
}
