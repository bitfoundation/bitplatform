using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Filters;

namespace Bit.Bmage.Masks;

/// <summary>How two masks combine, value by value.</summary>
/// <remarks>Soft masks combine as fuzzy sets: a union keeps the larger value, an intersection the smaller one.</remarks>
public enum MaskCombineMode
{
    /// <summary>Covered where either mask is: the larger of the two values.</summary>
    Union = 0,
    /// <summary>Covered where both masks are: the smaller of the two values.</summary>
    Intersect,
    /// <summary>Covered where the first mask is and the second is not.</summary>
    Subtract,
    /// <summary>Covered where exactly one of the masks is.</summary>
    Exclude,
}

/// <summary>
/// The pixels of a mask: one byte of coverage per pixel, 0 for untouched and 255 for fully affected, row-major and
/// tightly packed. A quarter of the memory of an <see cref="ImageBuffer"/> of the same size, and rented from the same
/// <see cref="IPixelAllocator"/>, so dispose it to return the memory.
/// </summary>
/// <remarks>
/// A <see cref="Mask"/> describes which pixels an edit affects; rendering it for an image produces one of these. They
/// can also be made directly, from an image's alpha or luminance, or painted by an application.
/// </remarks>
[DebuggerDisplay("{Width}x{Height} mask ({(IsDisposed ? \"disposed\" : \"live\")})")]
public sealed class MaskBuffer : IDisposable
{
    private IMemoryOwner<byte>? _owner;
    private Memory<byte> _memory;

    private MaskBuffer(int width, int height, IMemoryOwner<byte> owner)
    {
        Width = width;
        Height = height;
        _owner = owner;
        _memory = owner.Memory[..(width * height)];
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Size in pixels.</summary>
    public Size Size => new(Width, Height);

    /// <summary>Full bounds rectangle at origin.</summary>
    public Rectangle Bounds => new(0, 0, Width, Height);

    /// <summary>True once the buffer has been disposed.</summary>
    public bool IsDisposed => _memory.IsEmpty;

    /// <summary>Every coverage value, row-major.</summary>
    public Span<byte> Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetMemory().Span;
    }

    /// <summary>Gets the values of one row.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetRow(int y)
    {
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y), $"Row {y} is out of range.");
        return Values.Slice(y * Width, Width);
    }

    /// <summary>Gets or sets the coverage of one pixel. Bounds are checked.</summary>
    public byte this[int x, int y]
    {
        get
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(x), $"Pixel ({x},{y}) is out of range.");
            return Values[y * Width + x];
        }
        set
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(x), $"Pixel ({x},{y}) is out of range.");
            Values[y * Width + x] = value;
        }
    }

    /// <summary>Allocates a mask with every value set to <paramref name="fill"/>: 0, nothing covered, by default.</summary>
    public static MaskBuffer Create(int width, int height, byte fill = 0, IPixelAllocator? allocator = null, ImageLimits? limits = null)
    {
        (limits ?? ImageLimits.Default).Validate(width, height);
        var mask = new MaskBuffer(width, height, (allocator ?? PooledPixelAllocator.Shared).Rent(checked(width * height)));
        mask.Values.Fill(fill);
        return mask;
    }

    /// <summary>A mask of an image's alpha channel: opaque pixels are covered, transparent ones are not.</summary>
    public static MaskBuffer FromAlpha(ImageBuffer image, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        var mask = Create(image.Width, image.Height, 0, allocator, ImageLimits.Unrestricted);
        var values = mask.Values;
        var pixels = image.Pixels;
        for (var i = 0; i < values.Length; i++) values[i] = pixels[i].A;
        return mask;
    }

    /// <summary>
    /// A mask of an image's luminance (Rec. 709) weighted by its alpha: white covers, black and transparency do not.
    /// The usual way to turn a greyscale matte, such as a segmentation model's output, into a mask.
    /// </summary>
    public static MaskBuffer FromLuminance(ImageBuffer image, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        var mask = Create(image.Width, image.Height, 0, allocator, ImageLimits.Unrestricted);
        var values = mask.Values;
        var pixels = image.Pixels;
        for (var i = 0; i < values.Length; i++)
        {
            var p = pixels[i];
            values[i] = (byte)((Luma(p) * p.A + 127) / 255);
        }
        return mask;
    }

    /// <summary>Rec. 709 luma of a pixel in [0,255], in integers: 0.2126, 0.7152 and 0.0722 to within 1/256.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Luma(Rgba32 p) => (p.R * 54 + p.G * 183 + p.B * 19 + 128) >> 8;

    /// <summary>Sets every value.</summary>
    public void Fill(byte value) => Values.Fill(value);

    /// <summary>Creates an independent copy.</summary>
    public MaskBuffer Clone(IPixelAllocator? allocator = null)
    {
        var copy = Create(Width, Height, 0, allocator, ImageLimits.Unrestricted);
        Values.CopyTo(copy.Values);
        return copy;
    }

    /// <summary>Swaps covered and uncovered: every value becomes 255 minus itself.</summary>
    public void Invert()
    {
        var values = Values;
        for (var i = 0; i < values.Length; i++) values[i] = (byte)(255 - values[i]);
    }

    /// <summary>Multiplies every value by <paramref name="factor"/> in [0,1], fading the whole mask.</summary>
    public void Scale(float factor)
    {
        factor = Math.Clamp(factor, 0f, 1f);
        if (factor >= 1f) return;
        if (factor <= 0f)
        {
            Fill(0);
            return;
        }
        Span<byte> table = stackalloc byte[256];
        for (var v = 0; v < 256; v++) table[v] = Rgba32.ClampToByte(v * factor);
        var values = Values;
        for (var i = 0; i < values.Length; i++) values[i] = table[values[i]];
    }

    /// <summary>Combines another mask of the same size into this one.</summary>
    public void Combine(MaskBuffer other, MaskCombineMode mode)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Width != Width || other.Height != Height)
            throw new ArgumentException($"Cannot combine a {other.Width}x{other.Height} mask with a {Width}x{Height} one.", nameof(other));
        var a = Values;
        ReadOnlySpan<byte> b = other.Values;
        switch (mode)
        {
            case MaskCombineMode.Union:
                for (var i = 0; i < a.Length; i++) a[i] = Math.Max(a[i], b[i]);
                break;
            case MaskCombineMode.Intersect:
                for (var i = 0; i < a.Length; i++) a[i] = Math.Min(a[i], b[i]);
                break;
            case MaskCombineMode.Subtract:
                for (var i = 0; i < a.Length; i++) a[i] = Math.Min(a[i], (byte)(255 - b[i]));
                break;
            case MaskCombineMode.Exclude:
                for (var i = 0; i < a.Length; i++)
                    a[i] = Math.Max(Math.Min(a[i], (byte)(255 - b[i])), Math.Min((byte)(255 - a[i]), b[i]));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    /// <summary>
    /// Softens the mask's edges with a gaussian of the given standard deviation in pixels, as
    /// <see cref="Operations.Filters.BlurFilter"/> softens an image: three box passes, streamed so the memory is a few
    /// rows rather than a copy of the mask.
    /// </summary>
    public void Feather(float sigma, OperationContext? context = null)
    {
        if (!(sigma >= 0.05f)) return;
        // Past the mask's larger side a wider blur changes nothing more, and an absurd one, as an untrusted document may
        // hold, would only take long.
        sigma = Math.Min(sigma, Math.Max(Width, Height));
        // A pass among several of a render, so it reports no progress of its own.
        context = (context ?? OperationContext.Default).WithProgress(null);
        var radii = BoxBlur.BoxesForGauss(sigma, 3);
        var any = false;
        foreach (var radius in radii)
        {
            if (radius <= 0) continue;
            any = true;
            BoxHorizontal(radius, context);
            BoxVertical(radius, context);
        }
        // Three boxes of whole-pixel width cannot express a blur this small; one of radius 1 still softens the edge.
        if (!any)
        {
            BoxHorizontal(1, context);
            BoxVertical(1, context);
        }
    }

    /// <summary>
    /// How far along each axis <see cref="Feather"/> with <paramref name="sigma"/> carries a value: its passes' radii
    /// together. A value further than this from a pixel never reaches it, so a mask drawn this far around a window
    /// feathers to exactly the values the whole mask has there.
    /// </summary>
    internal static int FeatherReach(float sigma)
    {
        var reach = 0;
        foreach (var radius in BoxBlur.BoxesForGauss(sigma, 3))
            if (radius > 0) reach += radius;
        // Feather softens with one pass of radius 1 when three boxes cannot express the blur.
        return reach > 0 ? reach : 1;
    }

    /// <summary>A box average along every row, repeating the edge values beyond its ends.</summary>
    private void BoxHorizontal(int radius, OperationContext context)
    {
        var width = Width;
        var divisor = 2 * radius + 1;
        ParallelRows.For(context, 0, Height, width * 2L, (start, end, band) =>
        {
            var copy = ArrayPool<byte>.Shared.Rent(width);
            try
            {
                for (var y = start; y < end; y++)
                {
                    if (((y - start) & 63) == 0) band.ThrowIfCancellationRequested();
                    var row = GetRow(y);
                    var src = copy.AsSpan(0, width);
                    row.CopyTo(src);
                    // The edge value repeats beyond the row's end, so the opening sum needs no walk past it.
                    var inside = Math.Min(radius, width - 1);
                    var sum = (radius + 1) * src[0] + (radius - inside) * src[width - 1];
                    for (var j = 1; j <= inside; j++) sum += src[j];
                    for (var x = 0; x < width; x++)
                    {
                        row[x] = (byte)((sum + divisor / 2) / divisor);
                        sum += src[Math.Min(x + radius + 1, width - 1)] - src[Math.Max(x - radius, 0)];
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(copy);
            }
        });
    }

    /// <summary>Columns go in strips this wide, so a strip's running sums and saved rows stay small and the strips run side by side.</summary>
    private const int StripWidth = 256;

    /// <summary>
    /// A box average down every column, repeating the edge rows. Each strip of columns walks down the mask with one
    /// running sum per column, overwriting each row as it passes and keeping the last <c>radius + 1</c> original rows,
    /// which are all the sums still need.
    /// </summary>
    private void BoxVertical(int radius, OperationContext context)
    {
        var width = Width;
        var height = Height;
        var divisor = 2 * radius + 1;
        var strips = (width + StripWidth - 1) / StripWidth;
        var saved = Math.Min(radius + 1, height);
        ParallelRows.For(context, 0, strips, (long)StripWidth * height * 2, (first, last, band) =>
        {
            var sums = ArrayPool<int>.Shared.Rent(StripWidth);
            var ring = ArrayPool<byte>.Shared.Rent(saved * StripWidth);
            try
            {
                for (var strip = first; strip < last; strip++)
                {
                    band.ThrowIfCancellationRequested();
                    var x0 = strip * StripWidth;
                    var w = Math.Min(StripWidth, width - x0);
                    var values = Values;
                    // The original value of row r in this strip, wherever it is held.
                    var sum = sums.AsSpan(0, w);
                    for (var x = 0; x < w; x++)
                    {
                        var inside = Math.Min(radius, height - 1);
                        var v = (radius + 1) * values[x0 + x] + (radius - inside) * values[(height - 1) * width + x0 + x];
                        for (var j = 1; j <= inside; j++) v += values[j * width + x0 + x];
                        sum[x] = v;
                    }
                    for (var y = 0; y < height; y++)
                    {
                        if ((y & 255) == 255) band.ThrowIfCancellationRequested();
                        var row = values.Slice(y * width + x0, w);
                        // Save the original before it is overwritten; rows down to y - radius are read from here.
                        row.CopyTo(ring.AsSpan((y % saved) * StripWidth, w));
                        for (var x = 0; x < w; x++) row[x] = (byte)((sum[x] + divisor / 2) / divisor);
                        var add = Math.Min(y + radius + 1, height - 1);
                        var sub = Math.Max(y - radius, 0);
                        var addRow = add > y ? values.Slice(add * width + x0, w) : ring.AsSpan((add % saved) * StripWidth, w);
                        var subRow = ring.AsSpan((sub % saved) * StripWidth, w);
                        for (var x = 0; x < w; x++) sum[x] += addRow[x] - subRow[x];
                    }
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(sums);
                ArrayPool<byte>.Shared.Return(ring);
            }
        });
    }

    /// <summary>The smallest rectangle outside which every value is 0, or an empty rectangle when nothing is covered.</summary>
    public Rectangle GetBounds()
    {
        int top = -1, bottom = -1, left = Width, right = -1;
        for (var y = 0; y < Height; y++)
        {
            var row = GetRow(y);
            var first = row.IndexOfAnyExcept((byte)0);
            if (first < 0) continue;
            if (top < 0) top = y;
            bottom = y;
            left = Math.Min(left, first);
            right = Math.Max(right, row.LastIndexOfAnyExcept((byte)0));
        }
        return top < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    /// <summary>Multiplies the alpha of every pixel of <paramref name="image"/> by its coverage, cutting the image out.</summary>
    public void ApplyToAlpha(ImageBuffer image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width != Width || image.Height != Height)
            throw new ArgumentException($"A {Width}x{Height} mask cannot be applied to a {image.Width}x{image.Height} image.", nameof(image));
        var values = Values;
        var pixels = image.Pixels;
        for (var i = 0; i < values.Length; i++)
        {
            var m = values[i];
            if (m == 255) continue;
            var p = pixels[i];
            pixels[i] = p.WithAlpha((byte)((p.A * m + 127) / 255));
        }
    }

    /// <summary>
    /// The mask as an image, for showing it: <paramref name="color"/> (white by default) at full coverage, transparent
    /// where nothing is covered.
    /// </summary>
    public ImageBuffer ToImage(Rgba32? color = null, IPixelAllocator? allocator = null)
    {
        var tint = color ?? Rgba32.White;
        var image = ImageBuffer.Create(Width, Height, allocator, clear: false, ImageLimits.Unrestricted);
        var values = Values;
        var pixels = image.Pixels;
        for (var i = 0; i < values.Length; i++) pixels[i] = tint.WithAlpha((byte)((tint.A * values[i] + 127) / 255));
        return image;
    }

    /// <summary>
    /// A copy at another size: bilinear when enlarging, averaging whole blocks first when shrinking by more than half,
    /// so a low resolution matte from a segmentation model can cover a full resolution photo.
    /// </summary>
    public MaskBuffer Resize(int width, int height, IPixelAllocator? allocator = null)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
        if (width == Width && height == Height) return Clone(allocator);
        // Whole blocks are averaged down to near the target first, so shrinking by two or more never skips pixels.
        var fx = Math.Max(1, Width / width);
        var fy = Math.Max(1, Height / height);
        if (fx > 1 || fy > 1)
        {
            using var reduced = BoxReduce(fx, fy, allocator);
            return reduced.Resize(width, height, allocator);
        }
        var output = Create(width, height, 0, allocator, ImageLimits.Unrestricted);
        var sx = Width / (double)width;
        var sy = Height / (double)height;
        var src = Values;
        var dst = output.Values;
        for (var y = 0; y < height; y++)
        {
            var fyPos = Math.Clamp((y + 0.5) * sy - 0.5, 0, Height - 1);
            var y0 = (int)fyPos;
            var y1 = Math.Min(y0 + 1, Height - 1);
            var ty = fyPos - y0;
            for (var x = 0; x < width; x++)
            {
                var fxPos = Math.Clamp((x + 0.5) * sx - 0.5, 0, Width - 1);
                var x0 = (int)fxPos;
                var x1 = Math.Min(x0 + 1, Width - 1);
                var tx = fxPos - x0;
                var top = src[y0 * Width + x0] + (src[y0 * Width + x1] - src[y0 * Width + x0]) * tx;
                var bottom = src[y1 * Width + x0] + (src[y1 * Width + x1] - src[y1 * Width + x0]) * tx;
                dst[y * width + x] = (byte)Math.Round(top + (bottom - top) * ty);
            }
        }
        return output;
    }

    private MaskBuffer BoxReduce(int fx, int fy, IPixelAllocator? allocator)
    {
        var width = Math.Max(1, (Width + fx - 1) / fx);
        var height = Math.Max(1, (Height + fy - 1) / fy);
        var output = Create(width, height, 0, allocator, ImageLimits.Unrestricted);
        var src = Values;
        var dst = output.Values;
        for (var y = 0; y < height; y++)
        {
            var y0 = y * fy;
            var y1 = Math.Min(y0 + fy, Height);
            for (var x = 0; x < width; x++)
            {
                var x0 = x * fx;
                var x1 = Math.Min(x0 + fx, Width);
                var sum = 0;
                for (var yy = y0; yy < y1; yy++)
                    for (var xx = x0; xx < x1; xx++)
                        sum += src[yy * Width + xx];
                var count = (y1 - y0) * (x1 - x0);
                dst[y * width + x] = (byte)((sum + count / 2) / count);
            }
        }
        return output;
    }

    private Memory<byte> GetMemory()
    {
        var m = _memory;
        if (m.IsEmpty) throw new ObjectDisposedException(nameof(MaskBuffer));
        return m;
    }

    /// <summary>Releases the memory back to its allocator. Safe to call multiple times.</summary>
    public void Dispose()
    {
        _memory = default;
        var owner = _owner;
        _owner = null;
        owner?.Dispose();
    }
}
