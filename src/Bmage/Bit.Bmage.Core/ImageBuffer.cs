using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;

namespace Bit.Bmage;

/// <summary>
/// An uncompressed, tightly packed RGBA32 pixel buffer with straight (non-premultiplied) alpha.
/// This is the fundamental working representation for all processing in Bmage. Buffers are
/// usually rented from an <see cref="IPixelAllocator"/> and must be disposed to return memory to the pool.
/// </summary>
[DebuggerDisplay("{Width}x{Height} ({(IsDisposed ? \"disposed\" : \"live\")})")]
public sealed class ImageBuffer : IDisposable
{
    private IMemoryOwner<byte>? _owner;
    private Memory<byte> _memory;

    private ImageBuffer(int width, int height, IMemoryOwner<byte>? owner, Memory<byte> memory)
    {
        Width = width;
        Height = height;
        _owner = owner;
        _memory = memory;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Size in pixels.</summary>
    public Size Size => new(Width, Height);

    /// <summary>Full bounds rectangle at origin.</summary>
    public Rectangle Bounds => new(0, 0, Width, Height);

    /// <summary>Number of bytes per row (always <c>Width * 4</c>; rows are tightly packed).</summary>
    public int Stride => Width * 4;

    /// <summary>Total number of pixel bytes.</summary>
    public int ByteLength => Width * Height * 4;

    /// <summary>Total pixel count.</summary>
    public int PixelCount => Width * Height;

    /// <summary>Metadata associated with the image (EXIF, ICC profile, source format). May be null.</summary>
    public ImageMetadata? Metadata { get; set; }

    /// <summary>True once the buffer has been disposed.</summary>
    public bool IsDisposed => _memory.IsEmpty && Width > 0;

    /// <summary>All pixel bytes in R,G,B,A order, row-major, tightly packed.</summary>
    public Span<byte> Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetMemory().Span;
    }

    /// <summary>All pixels, row-major.</summary>
    public Span<Rgba32> Pixels
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.Cast<byte, Rgba32>(GetMemory().Span);
    }

    /// <summary>The underlying memory. Prefer <see cref="Bytes"/> for synchronous access.</summary>
    public Memory<byte> Memory => GetMemory();

    /// <summary>Allocates a new buffer. When <paramref name="clear"/> is true the pixels are initialised to transparent black.</summary>
    public static ImageBuffer Create(int width, int height, IPixelAllocator? allocator = null, bool clear = true, ImageLimits? limits = null)
    {
        (limits ?? ImageLimits.Default).Validate(width, height);
        var byteCount = checked(width * height * 4);
        var owner = (allocator ?? PooledPixelAllocator.Shared).Rent(byteCount);
        var memory = owner.Memory[..byteCount];
        if (clear) memory.Span.Clear();
        return new ImageBuffer(width, height, owner, memory);
    }

    /// <summary>Allocates a buffer filled with a solid colour.</summary>
    public static ImageBuffer Create(int width, int height, Rgba32 fill, IPixelAllocator? allocator = null)
    {
        var buffer = Create(width, height, allocator, clear: false);
        buffer.Fill(fill);
        return buffer;
    }

    /// <summary>
    /// Wraps existing memory without taking ownership. The memory must contain exactly
    /// <c>width * height * 4</c> bytes of tightly packed RGBA pixels.
    /// </summary>
    public static ImageBuffer Wrap(Memory<byte> memory, int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
        if (memory.Length != checked(width * height * 4))
            throw new ArgumentException($"Memory length {memory.Length} does not match {width}x{height}x4 = {width * height * 4}.", nameof(memory));
        return new ImageBuffer(width, height, null, memory);
    }

    /// <summary>Wraps a byte array without copying. The array must be exactly <c>width * height * 4</c> bytes.</summary>
    public static ImageBuffer Wrap(byte[] pixels, int width, int height) => Wrap(pixels.AsMemory(), width, height);

    /// <summary>Copies raw RGBA bytes into a newly allocated buffer.</summary>
    public static ImageBuffer FromRgba(ReadOnlySpan<byte> pixels, int width, int height, IPixelAllocator? allocator = null)
    {
        var buffer = Create(width, height, allocator, clear: false);
        if (pixels.Length != buffer.ByteLength)
        {
            buffer.Dispose();
            throw new ArgumentException($"Pixel data length {pixels.Length} does not match {width}x{height}x4.", nameof(pixels));
        }
        pixels.CopyTo(buffer.Bytes);
        return buffer;
    }

    /// <summary>Gets the span for one row.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Rgba32> GetRow(int y)
    {
        if ((uint)y >= (uint)Height) ThrowRow(y);
        return Pixels.Slice(y * Width, Width);
    }

    /// <summary>Gets the raw bytes for one row.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetRowBytes(int y)
    {
        if ((uint)y >= (uint)Height) ThrowRow(y);
        return Bytes.Slice(y * Stride, Stride);
    }

    /// <summary>Gets or sets one pixel. Bounds are checked.</summary>
    public Rgba32 this[int x, int y]
    {
        get
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) ThrowXY(x, y);
            return Pixels[y * Width + x];
        }
        set
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) ThrowXY(x, y);
            Pixels[y * Width + x] = value;
        }
    }

    /// <summary>Fills the whole buffer with a colour.</summary>
    public void Fill(Rgba32 color) => Pixels.Fill(color);

    /// <summary>Fills a rectangular region (clipped to the buffer bounds) with a colour, replacing existing pixels.</summary>
    public void Fill(Rectangle rect, Rgba32 color)
    {
        rect.Intersect(Bounds);
        if (rect.IsEmptyArea()) return;
        for (var y = rect.Top; y < rect.Bottom; y++)
            GetRow(y).Slice(rect.Left, rect.Width).Fill(color);
    }

    /// <summary>Creates an independent copy of this buffer including metadata.</summary>
    public ImageBuffer Clone(IPixelAllocator? allocator = null)
    {
        // This buffer already exists, so whatever limits it was created under were met; re-checking against the
        // defaults would refuse to copy an image that was deliberately allowed past them.
        var copy = Create(Width, Height, allocator, clear: false, ImageLimits.Unrestricted);
        Bytes.CopyTo(copy.Bytes);
        copy.Metadata = Metadata;
        return copy;
    }

    /// <summary>Copies a rectangular region into a new buffer. The rectangle is clipped to the source bounds.</summary>
    public ImageBuffer CopyRegion(Rectangle rect, IPixelAllocator? allocator = null)
    {
        rect.Intersect(Bounds);
        if (rect.IsEmptyArea()) throw new ArgumentException("Region does not intersect the image.", nameof(rect));
        var copy = Create(rect.Width, rect.Height, allocator, clear: false, ImageLimits.Unrestricted);
        for (var y = 0; y < rect.Height; y++)
            GetRow(rect.Y + y).Slice(rect.X, rect.Width).CopyTo(copy.GetRow(y));
        copy.Metadata = Metadata;
        return copy;
    }

    /// <summary>Copies all pixels into <paramref name="destination"/>, which must have identical dimensions.</summary>
    public void CopyTo(ImageBuffer destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Width != Width || destination.Height != Height)
            throw new ArgumentException("Destination dimensions do not match.", nameof(destination));
        Bytes.CopyTo(destination.Bytes);
    }

    /// <summary>
    /// Copies pixels from this buffer into <paramref name="destination"/> at the given offset without blending.
    /// Regions outside the destination are clipped.
    /// </summary>
    public void CopyTo(ImageBuffer destination, Point offset)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var target = new Rectangle(offset, Size);
        target.Intersect(destination.Bounds);
        if (target.IsEmptyArea()) return;
        var sx = target.X - offset.X;
        var sy = target.Y - offset.Y;
        for (var y = 0; y < target.Height; y++)
            GetRow(sy + y).Slice(sx, target.Width).CopyTo(destination.GetRow(target.Y + y).Slice(target.X, target.Width));
    }

    /// <summary>Returns true if any pixel has an alpha value below 255.</summary>
    public bool HasTransparency()
    {
        var bytes = Bytes;
        for (var i = 3; i < bytes.Length; i += 4)
            if (bytes[i] != 255) return true;
        return false;
    }

    /// <summary>Returns a copy of the pixel bytes as a new array.</summary>
    public byte[] ToArray() => Bytes.ToArray();

    /// <summary>
    /// Transfers ownership of the underlying memory to the caller and disposes this wrapper.
    /// The returned owner may be larger than <see cref="ByteLength"/>.
    /// </summary>
    public IMemoryOwner<byte> DetachMemory()
    {
        var owner = _owner ?? throw new InvalidOperationException("This buffer does not own its memory.");
        _owner = null;
        _memory = default;
        return owner;
    }

    private Memory<byte> GetMemory()
    {
        var m = _memory;
        if (m.IsEmpty) ThrowDisposed();
        return m;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowDisposed() => throw new ObjectDisposedException(nameof(ImageBuffer));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowRow(int y) => throw new ArgumentOutOfRangeException(nameof(y), $"Row {y} is out of range.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowXY(int x, int y) => throw new ArgumentOutOfRangeException($"Pixel ({x},{y}) is out of range.");

    /// <summary>Releases the pixel memory back to its allocator. Safe to call multiple times.</summary>
    public void Dispose()
    {
        _memory = default;
        var owner = _owner;
        _owner = null;
        owner?.Dispose();
    }
}
