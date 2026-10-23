using System.Drawing;

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
        var background = options.Color ?? image.GetRow(0)[0];
        bool IsBackground(Rgba32 p) => transparent
            ? p.A <= tolerance
            : Math.Abs(p.R - background.R) <= tolerance && Math.Abs(p.G - background.G) <= tolerance
                && Math.Abs(p.B - background.B) <= tolerance && Math.Abs(p.A - background.A) <= tolerance;

        // Rows from the top and the bottom until one holds content, then only the columns outside the content found so
        // far: the cost is the border's pixels, not the image's.
        int width = image.Width, height = image.Height;
        var top = 0;
        while (top < height && RowIsBackground(image.GetRow(top), IsBackground))
        {
            if ((top & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            top++;
        }
        if (top == height) return null;
        var bottom = height - 1;
        while (bottom > top && RowIsBackground(image.GetRow(bottom), IsBackground))
        {
            if ((bottom & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            bottom--;
        }
        int left = width, right = -1;
        for (var y = top; y <= bottom; y++)
        {
            if (((y - top) & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = image.GetRow(y);
            for (var x = 0; x < left; x++)
            {
                if (IsBackground(row[x])) continue;
                left = x;
                break;
            }
            for (var x = width - 1; x > right; x--)
            {
                if (IsBackground(row[x])) continue;
                right = x;
                break;
            }
        }

        var bounds = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        bounds.Inflate(options.Padding, options.Padding);
        bounds.Intersect(image.Bounds);
        return bounds;
    }

    private static bool RowIsBackground(ReadOnlySpan<Rgba32> row, Func<Rgba32, bool> isBackground)
    {
        foreach (var p in row)
            if (!isBackground(p)) return false;
        return true;
    }

    private static bool HasTransparentCorner(ImageBuffer image, int tolerance)
    {
        var top = image.GetRow(0);
        var bottom = image.GetRow(image.Height - 1);
        return top[0].A <= tolerance || top[^1].A <= tolerance || bottom[0].A <= tolerance || bottom[^1].A <= tolerance;
    }
}
