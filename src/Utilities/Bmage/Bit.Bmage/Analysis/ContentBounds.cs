using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bit.Bmage.Analysis;

/// <summary>What counts as background when trimming an image.</summary>
public enum TrimMode
{
    /// <summary>
    /// Transparent pixels when any corner of the image is transparent, otherwise the colour of the top left pixel: what
    /// a cut-out on transparency and a scan on a white page each expect.
    /// </summary>
    Auto = 0,
    /// <summary>Pixels whose alpha is within the tolerance of fully transparent.</summary>
    Transparent,
    /// <summary>Pixels within the tolerance of <see cref="TrimOptions.Color"/>, or of the top left pixel when that is null.</summary>
    Color,
}

/// <summary>How <see cref="ContentBounds.Find"/> tells content from background.</summary>
public sealed record TrimOptions
{
    /// <summary>Transparent or top-left-colour borders, within 4% per channel.</summary>
    public static TrimOptions Default { get; } = new();

    /// <summary>What counts as background.</summary>
    public TrimMode Mode { get; init; }

    /// <summary>The background colour for <see cref="TrimMode.Color"/>, or null to take the top left pixel's.</summary>
    public Rgba32? Color { get; init; }

    /// <summary>
    /// How far a channel may differ from the background and still count as background, in [0,1] of the channel's range.
    /// A little tolerance absorbs JPEG noise in a "white" border.
    /// </summary>
    public float Tolerance
    {
        get;
        init => field = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : throw new ArgumentOutOfRangeException(nameof(Tolerance), "The tolerance must be a finite number.");
    } = 0.04f;

    /// <summary>Pixels of background kept around the content on every side, as far as the image reaches.</summary>
    public int Padding
    {
        get;
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Padding), "The padding cannot be negative.");
    }
}

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
/// var pipeline = ImagePipeline.Create();
/// if (ContentBounds.Find(image, new TrimOptions { Padding = 8 }) is { } content)
///     pipeline = pipeline.Crop(content);
/// using var trimmed = pipeline.Resize(1000, 1000, ResizeMode.Contain, background: Rgba32.White).Execute(image);
/// </code>
/// </example>
public static class ContentBounds
{
    /// <summary>
    /// The smallest rectangle holding every pixel that is not background, grown by the padding, or null when the
    /// whole image is background.
    /// </summary>
    public static Rectangle? Find(ImageBuffer image, TrimOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= TrimOptions.Default;
        var tolerance = (int)MathF.Round(options.Tolerance * 255);
        var transparent = options.Mode switch
        {
            TrimMode.Transparent => true,
            TrimMode.Color => false,
            _ => HasTransparentCorner(image, tolerance),
        };
        // Transparency is background whatever its colour: every colour channel may differ entirely.
        var background = new Background(transparent ? default : options.Color ?? image.GetRow(0)[0],
            transparent ? new Rgba32(255, 255, 255, (byte)tolerance) : new Rgba32((byte)tolerance, (byte)tolerance, (byte)tolerance, (byte)tolerance));

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
    private readonly struct Background(Rgba32 color, Rgba32 tolerance)
    {
        private readonly Vector128<byte> _color = Vector128.Create(color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A, color.R, color.G, color.B, color.A);
        private readonly Vector128<byte> _tolerance = Vector128.Create(tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A, tolerance.R, tolerance.G, tolerance.B, tolerance.A);

        public bool Matches(Rgba32 p)
            => Math.Abs(p.R - color.R) <= tolerance.R && Math.Abs(p.G - color.G) <= tolerance.G
               && Math.Abs(p.B - color.B) <= tolerance.B && Math.Abs(p.A - color.A) <= tolerance.A;

        /// <summary>The index of the first pixel that is not background, or -1.</summary>
        public int FirstContent(ReadOnlySpan<Rgba32> row)
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
        public int LastContent(ReadOnlySpan<Rgba32> row)
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

    private static bool HasTransparentCorner(ImageBuffer image, int tolerance)
    {
        var top = image.GetRow(0);
        var bottom = image.GetRow(image.Height - 1);
        return top[0].A <= tolerance || top[^1].A <= tolerance || bottom[0].A <= tolerance || bottom[^1].A <= tolerance;
    }
}
