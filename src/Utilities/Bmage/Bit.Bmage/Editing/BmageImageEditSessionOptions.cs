using System.Drawing;

namespace Bit.Bmage;

/// <summary>Options for an <see cref="BmageImageEditSession"/>.</summary>
public sealed class BmageImageEditSessionOptions
{
    /// <summary>
    /// Longest edge of the interactive proxy. 2048 keeps editing smooth on phones while staying sharp on a desktop
    /// display. Set to 0 to always edit at full resolution.
    /// </summary>
    public int MaxProxyDimension { get; set; } = 2048;

    /// <summary>Number of undo steps kept.</summary>
    public int MaxHistoryDepth { get; set; } = 100;

    /// <summary>Allocator used for proxies and previews.</summary>
    public IBmagePixelAllocator? Allocator { get; set; }

    /// <summary>Limits applied to rendering.</summary>
    public BmageImageLimits? Limits { get; set; }

    /// <summary>Rasteriser used for text annotations.</summary>
    public IBmageTextRasterizer? TextRasterizer { get; set; }

    /// <summary>
    /// The most threads one operation may spread its rows over while rendering, or null for
    /// <see cref="BmageOperationContext.DefaultMaxDegreeOfParallelism"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        set => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>
    /// Builds the working proxy, or returns null to leave it to the managed resampler. The editor supplies the
    /// browser's resampler here on WebAssembly. The preview is all it affects: export always replays the edit on the
    /// original pixels.
    /// </summary>
    internal Func<BmageImageBuffer, Size, CancellationToken, ValueTask<BmageImageBuffer?>>? ProxyScaler { get; set; }
}
