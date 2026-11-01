using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bit.BbScanner;

/// <summary>
/// A non-owning, rectangular window over an 8 bit grayscale image.
/// </summary>
/// <remarks>
/// The view carries no ownership, so cropping, rotating the sampling origin or handing a
/// sub-region to a decoder never copies pixels. All decoders in the pipeline accept a view
/// rather than a buffer so that the scan region feature costs nothing at run time.
/// </remarks>
public readonly struct BbLuminanceView
{
    private readonly byte[] _data;
    private readonly int _offset;
    private readonly int _stride;

    /// <summary>Creates a view over the whole of <paramref name="data"/>.</summary>
    /// <param name="data">Row-major grayscale samples.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public BbLuminanceView(byte[] data, int width, int height)
        : this(data, 0, width, width, height)
    {
    }

    /// <summary>Creates a view over a sub-region of <paramref name="data"/>.</summary>
    /// <param name="data">Row-major grayscale samples.</param>
    /// <param name="offset">Index of the top-left sample of the window.</param>
    /// <param name="stride">Distance in samples between the starts of two consecutive rows.</param>
    /// <param name="width">Window width in pixels.</param>
    /// <param name="height">Window height in pixels.</param>
    public BbLuminanceView(byte[] data, int offset, int stride, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        // The first and last rows' ends in 64 bits: near the largest int an end wraps round to a small number, which
        // passes for one inside the buffer.
        var last = offset + ((long)(height - 1) * stride);
        if (height > 0 && (offset < 0 || last < 0 || Math.Max(offset, last) + width > data.Length))
        {
            throw new ArgumentOutOfRangeException(nameof(data), "The window does not fit inside the supplied buffer.");
        }

        _data = data;
        _offset = offset;
        _stride = stride;
        Width = width;
        Height = height;
    }

    /// <summary>Window width in pixels.</summary>
    public int Width { get; }

    /// <summary>Window height in pixels.</summary>
    public int Height { get; }

    /// <summary><see langword="true"/> when the window has no pixels.</summary>
    public bool IsEmpty => Width == 0 || Height == 0;

    /// <summary>Distance in samples between the starts of two consecutive rows of the underlying buffer.</summary>
    public int Stride => _stride;

    /// <summary>Index of the top-left sample of the window inside the underlying buffer.</summary>
    public int Offset => _offset;

    /// <summary>The underlying buffer. Exposed for zero-copy interop; prefer <see cref="GetRow"/>.</summary>
    public byte[] Buffer => _data;

    /// <summary>Returns the samples of row <paramref name="y"/>.</summary>
    /// <param name="y">Row index relative to the window.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetRow(int y)
    {
        Debug.Assert((uint)y < (uint)Height);
        return _data.AsSpan(_offset + (y * _stride), Width);
    }

    /// <summary>Reads a single sample.</summary>
    /// <param name="x">Column index relative to the window.</param>
    /// <param name="y">Row index relative to the window.</param>
    public byte this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert((uint)x < (uint)Width && (uint)y < (uint)Height);
            return _data[_offset + (y * _stride) + x];
        }
    }

    /// <summary>
    /// Reads the luminance at a point between samples, interpolated bilinearly between the four round it. Sample
    /// (<c>x</c>, <c>y</c>) is at the point (<c>x</c>, <c>y</c>) - a caller measuring from pixel edges, where a
    /// pixel's centre is half a pixel in, subtracts that half first - and a point beyond the outermost samples reads
    /// as the nearest one at the edge.
    /// </summary>
    /// <param name="x">Horizontal position relative to the window.</param>
    /// <param name="y">Vertical position relative to the window.</param>
    /// <remarks>The window must not be empty.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal float Interpolate(float x, float y)
    {
        Debug.Assert(!IsEmpty);
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        var (x0, y0) = ((int)x, (int)y);
        var (x1, y1) = (Math.Min(x0 + 1, Width - 1), Math.Min(y0 + 1, Height - 1));
        var (tx, ty) = (x - x0, y - y0);
        var top = (this[x0, y0] * (1 - tx)) + (this[x1, y0] * tx);
        var bottom = (this[x0, y1] * (1 - tx)) + (this[x1, y1] * tx);
        return (top * (1 - ty)) + (bottom * ty);
    }

    /// <summary>Returns a view of a sub-region of this window, without copying.</summary>
    /// <param name="x">Left edge relative to this window.</param>
    /// <param name="y">Top edge relative to this window.</param>
    /// <param name="width">Sub-region width.</param>
    /// <param name="height">Sub-region height.</param>
    public BbLuminanceView Crop(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        // Subtracted rather than added: a sum near the largest int wraps round to a small one, which passes.
        if (x > Width - width || y > Height - height)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The crop lies outside the current window.");
        }

        return new BbLuminanceView(_data, _offset + (y * _stride) + x, _stride, width, height);
    }

    /// <summary>Returns the sub-region described by a normalized <see cref="BbScanRegion"/>.</summary>
    /// <param name="region">Region in normalized coordinates, resolved against this window's size.</param>
    public BbLuminanceView Crop(BbScanRegion region)
    {
        // A region whose edge sits exactly on the far side of the frame, or one with no area,
        // still yields a window inside the buffer.
        var (x, y, w, h) = region.ToPixels(Width, Height);
        return x == 0 && y == 0 && w == Width && h == Height ? this : Crop(x, y, w, h);
    }
}
