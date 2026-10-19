using Bit.Bmage.Memory;

namespace Bit.Bmage;

/// <summary>Options for the Bmage services.</summary>
public sealed class BitBmageOptions
{
    /// <summary>Safety limits applied to decoding and processing. Defaults to <see cref="ImageLimits.Default"/>.</summary>
    public ImageLimits Limits { get; set; } = ImageLimits.Default;

    /// <summary>Allocator used for pixel buffers. Defaults to the shared pooled allocator.</summary>
    public IPixelAllocator? Allocator { get; set; }

    /// <summary>Maximum encoded bytes accepted for a single uploaded image. Defaults to 64 MB.</summary>
    public long MaxSourceBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>
    /// Largest working resolution used for interactive editing. Larger images are edited through a downscaled proxy and
    /// the edit is replayed at full resolution on export. Null uses the device-appropriate default.
    /// </summary>
    public int? MaxInteractiveDimension { get; set; }
}
