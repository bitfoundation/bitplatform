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

        if (height > 0 && (offset + ((height - 1) * stride) + width) > data.Length)
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

    /// <summary>Returns a view of a sub-region of this window, without copying.</summary>
    /// <param name="x">Left edge relative to this window.</param>
    /// <param name="y">Top edge relative to this window.</param>
    /// <param name="width">Sub-region width.</param>
    /// <param name="height">Sub-region height.</param>
    public BbLuminanceView Crop(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        if (x + width > Width || y + height > Height)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The crop lies outside the current window.");
        }

        return new BbLuminanceView(_data, _offset + (y * _stride) + x, _stride, width, height);
    }

    /// <summary>Returns the sub-region described by a normalized <see cref="BbScanRegion"/>.</summary>
    /// <param name="region">Region in normalized coordinates.</param>
    public BbLuminanceView Crop(BbScanRegion region)
    {
        if (region.IsFull)
        {
            return this;
        }

        region = region.Normalized();

        // A region whose edge sits exactly on the far side of the frame, or one with no area,
        // still has to yield a window inside the buffer: the pipeline treats "nothing to scan"
        // as a frame with no symbol, never as an exception.
        var x = Math.Clamp((int)(region.X * Width), 0, Math.Max(0, Width - 1));
        var y = Math.Clamp((int)(region.Y * Height), 0, Math.Max(0, Height - 1));
        var w = Math.Clamp((int)(region.Width * Width), 1, Math.Max(1, Width - x));
        var h = Math.Clamp((int)(region.Height * Height), 1, Math.Max(1, Height - y));
        return Crop(x, y, w, h);
    }
}
