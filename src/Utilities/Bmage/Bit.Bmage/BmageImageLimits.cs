namespace Bit.Bmage;

/// <summary>
/// Safety limits applied when decoding and allocating images. Protects against decompression bombs,
/// absurd dimensions and memory exhaustion. All limits are inclusive.
/// </summary>
public sealed record BmageImageLimits
{
    /// <summary>Default limits: 16384px per side, 64 megapixels, 256 MB encoded input.</summary>
    public static BmageImageLimits Default { get; } = new();

    /// <summary>Permissive limits for trusted environments (e.g. server-side processing of known files).</summary>
    public static BmageImageLimits Unrestricted { get; } = new()
    {
        MaxWidth = int.MaxValue,
        MaxHeight = int.MaxValue,
        MaxPixels = long.MaxValue,
        MaxEncodedBytes = long.MaxValue,
    };

    /// <summary>Maximum width in pixels of a decoded or allocated image.</summary>
    public int MaxWidth { get; init; } = 16384;

    /// <summary>Maximum height in pixels of a decoded or allocated image.</summary>
    public int MaxHeight { get; init; } = 16384;

    /// <summary>Maximum total number of pixels (width × height).</summary>
    public long MaxPixels { get; init; } = 64L * 1024 * 1024;

    /// <summary>Maximum size in bytes of an encoded image accepted for decoding.</summary>
    public long MaxEncodedBytes { get; init; } = 256L * 1024 * 1024;

    /// <summary>Returns true if an image with the given dimensions is within the limits.</summary>
    public bool Allows(int width, int height)
        => width > 0 && height > 0 && width <= MaxWidth && height <= MaxHeight && (long)width * height <= MaxPixels;

    /// <summary>Throws <see cref="BmageImageLimitExceededException"/> when the dimensions exceed the limits.</summary>
    public void Validate(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), $"Image dimensions must be positive (got {width}x{height}).");
        if (!Allows(width, height))
            throw new BmageImageLimitExceededException($"Image dimensions {width}x{height} exceed the configured limits (max {MaxWidth}x{MaxHeight}, {MaxPixels} pixels).");
    }

    /// <summary>Throws <see cref="BmageImageLimitExceededException"/> when the encoded size exceeds the limit.</summary>
    public void ValidateEncodedSize(long bytes)
    {
        if (bytes > MaxEncodedBytes)
            throw new BmageImageLimitExceededException($"Encoded image size {bytes} bytes exceeds the configured limit of {MaxEncodedBytes} bytes.");
    }
}
