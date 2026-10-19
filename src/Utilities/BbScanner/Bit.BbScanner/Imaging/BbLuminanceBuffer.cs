using System.Buffers;

namespace Bit.BbScanner;

/// <summary>
/// Owns a pooled 8 bit grayscale image buffer.
/// </summary>
/// <remarks>
/// Continuous scanning decodes tens of frames per second, so every frame allocating a fresh
/// managed array would dominate the GC budget of a WebAssembly application. Buffers are rented
/// from <see cref="ArrayPool{T}.Shared"/> and returned on <see cref="Dispose"/>; the scanner
/// keeps a single buffer alive for the lifetime of a session and only re-rents when the camera
/// resolution changes.
/// </remarks>
public sealed class BbLuminanceBuffer : IDisposable
{
    private byte[]? _data;

    private BbLuminanceBuffer(byte[] data, int width, int height)
    {
        _data = data;
        Width = width;
        Height = height;
    }

    /// <summary>Image width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Image height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>Rents a buffer able to hold a <paramref name="width"/> by <paramref name="height"/> image.</summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public static BbLuminanceBuffer Rent(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        return new BbLuminanceBuffer(ArrayPool<byte>.Shared.Rent(width * height), width, height);
    }

    /// <summary>Creates a buffer that wraps an existing array without copying or pooling it.</summary>
    /// <param name="data">Row-major grayscale samples, at least <c>width * height</c> long.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public static BbLuminanceBuffer Wrap(byte[] data, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < width * height)
        {
            throw new ArgumentException("Buffer is smaller than the declared image size.", nameof(data));
        }

        return new BbLuminanceBuffer(data, width, height) { _pooled = false };
    }

    private bool _pooled = true;

    /// <summary>Ensures the buffer can hold the requested size, re-renting only when it has to grow.</summary>
    /// <param name="width">Required width in pixels.</param>
    /// <param name="height">Required height in pixels.</param>
    public void Resize(int width, int height)
    {
        ObjectDisposedException.ThrowIf(_data is null, this);
        var required = width * height;
        if (_data.Length < required)
        {
            if (_pooled)
            {
                ArrayPool<byte>.Shared.Return(_data);
            }

            _data = ArrayPool<byte>.Shared.Rent(required);
            _pooled = true;
        }

        Width = width;
        Height = height;
    }

    /// <summary>The writable samples of the image.</summary>
    public Span<byte> Pixels
    {
        get
        {
            ObjectDisposedException.ThrowIf(_data is null, this);
            return _data.AsSpan(0, Width * Height);
        }
    }

    /// <summary>The underlying array. Its length may exceed <c>Width * Height</c> because it is pooled.</summary>
    public byte[] Array
    {
        get
        {
            ObjectDisposedException.ThrowIf(_data is null, this);
            return _data;
        }
    }

    /// <summary>A view over the whole image.</summary>
    public BbLuminanceView View
    {
        get
        {
            ObjectDisposedException.ThrowIf(_data is null, this);
            return new BbLuminanceView(_data, 0, Width, Width, Height);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        var data = _data;
        _data = null;
        if (data is not null && _pooled)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
